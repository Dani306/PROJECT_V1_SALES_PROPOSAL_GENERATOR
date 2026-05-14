using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PROJECT_V1.Models;
using PROJECT_V1.Infrastructure;

namespace PROJECT_V1.Services;

public sealed class GeminiProposalService(
    HttpClient httpClient,
    IOptions<LlmOptions> options,
    IOptions<ProposalGeneratorOptions> proposalOptions,
    ILogger<GeminiProposalService> logger) : IProposalService
{
    private readonly GeminiSettings _settings = options.Value.Gemini;
    private readonly ProposalGeneratorOptions _genOptions = proposalOptions.Value;

    public async Task<Result<ProposalResponse>> GenerateProposalAsync(
        ProposalRequest request, 
        CancellationToken ct = default)
    {
        var apiKey = ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            return "Configuration error: API Key is missing.";

        // Use a PathString or specialized URI builder to prevent slash issues
        var uri = $"{_settings.Model}:generateContent?key={apiKey}";

        try
        {
            // 1. Optimized Payload construction using internal DTOs
            var payload = CreateRequestPayload(request);

            // 2. Use Source-Generated JSON context for zero-reflection serialization
            using var response = await httpClient.PostAsJsonAsync(
                uri, 
                payload, 
                GeminiJsonContext.Default.GeminiRequest, 
                ct);

            if (!response.IsSuccessStatusCode)
                return await HandleApiError(response, ct);

            // 3. Stream direct to DTO to keep memory footprint low
            var data = await response.Content.ReadFromJsonAsync(
                GeminiJsonContext.Default.GeminiRawResponse, 
                ct);

            return data?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text switch
            {
                { Length: > 0 } text => ProposalResponseParser.Parse(text),
                _ => "The AI generated an empty response. Please adjust your prompt."
            };
        }
        catch (OperationCanceledException)
        {
            return "The request timed out. Please try again.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gemini service fault for client: {Client}", request.ClientName);
            return "A technical fault occurred. Support has been notified.";
        }
    }

    private GeminiRequest CreateRequestPayload(ProposalRequest request)
    {
        var (system, user) = ProposalPromptBuilder.BuildPrompt(request);
        return new GeminiRequest(
            [new Content("user", [new Part($"{system}\n\n{user}")])],
            new GenerationConfig(_genOptions.Temperature, _genOptions.MaxOutputTokens)
        );
    }

    private async Task<string> HandleApiError(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        logger.LogError("Gemini API Failure | Status: {Status} | Response: {Body}", response.StatusCode, body);
        
        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "AI Service authentication failed.",
            System.Net.HttpStatusCode.TooManyRequests => "Rate limit exceeded. Please wait a moment.",
            _ => "The AI service is currently unavailable."
        };
    }

    private string ResolveApiKey() => Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? _settings.ApiKey;
}
