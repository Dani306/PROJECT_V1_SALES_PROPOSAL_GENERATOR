using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PROJECT_V1.Models;
using PROJECT_V1.Infrastructure; // Assuming Result<T> is here

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
            return Result.Failure("Gemini API Key is missing.");

        var payload = CreatePayload(request);
        var url = $"{_settings.Model}:generateContent?key={apiKey}";

        try
        {
            // PostAsJsonAsync is more efficient than manual string content creation
            using var response = await httpClient.PostAsJsonAsync(url, payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Gemini API Error {Status}: {Body}", response.StatusCode, error);
                return Result.Failure("The AI service returned an error. Please try again.");
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiRawResponse>(cancellationToken: ct);
            var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            return text switch
            {
                { Length: > 0 } => ProposalResponseParser.Parse(text),
                _ => Result.Failure("AI returned an empty response.")
            };
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Unexpected failure in Gemini Service");
            return Result.Failure("A technical error occurred.");
        }
    }

    private object CreatePayload(ProposalRequest request)
    {
        var (system, user) = ProposalPromptBuilder.BuildPrompt(request);
        return new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = $"{system}\n\n{user}" } } } },
            generationConfig = new
            {
                temperature = _genOptions.Temperature,
                maxOutputTokens = _genOptions.MaxOutputTokens,
                responseMimeType = "application/json"
            }
        };
    }

    private string ResolveApiKey() => Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? _settings.ApiKey;
}
