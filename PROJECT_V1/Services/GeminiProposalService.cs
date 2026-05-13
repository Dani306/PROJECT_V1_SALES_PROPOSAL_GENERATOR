using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PROJECT_V1.Models;
using PROJECT_V1.Infrastructure; // Assuming Result<T> lives here

namespace PROJECT_V1.Services;

public sealed class GeminiProposalService : IProposalService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ProposalGeneratorOptions _proposalOptions;
    private readonly ILogger<GeminiProposalService> _logger;

    public GeminiProposalService(
        HttpClient httpClient, // Using a Typed Client
        IOptions<LlmOptions> options,
        IOptions<ProposalGeneratorOptions> proposalOptions,
        ILogger<GeminiProposalService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value.Gemini;
        _proposalOptions = proposalOptions.Value;
        _logger = logger;
    }

    public async Task<Result<ProposalResponse>> GenerateProposalAsync(
        ProposalRequest request, 
        CancellationToken ct = default)
    {
        var apiKey = ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            return "API Key is missing. Check your configuration.";

        var payload = BuildPayload(request);
        var endpoint = $"{_settings.Model}:generateContent?key={apiKey}";

        try
        {
            // Use PostAsJsonAsync to avoid manual string serialization/encoding
            using var response = await _httpClient.PostAsJsonAsync(endpoint, payload, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Gemini API Error: {Status} - {Body}", response.StatusCode, errorBody);
                return "The AI service encountered an error. Please try again later.";
            }

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiRawResponse>(cancellationToken: ct);
            
            return ExtractText(geminiResponse) switch
            {
                { } text => ProposalResponseParser.Parse(text),
                null => "The AI returned an empty or invalid response."
            };
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unrecoverable failure in Proposal Service");
            return "A technical error occurred during proposal generation.";
        }
    }

    private object BuildPayload(ProposalRequest request)
    {
        var (system, user) = ProposalPromptBuilder.BuildPrompt(request);
        return new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = $"{system}\n\n{user}" } } } },
            generationConfig = new
            {
                temperature = _proposalOptions.Temperature,
                maxOutputTokens = _proposalOptions.MaxOutputTokens,
                responseMimeType = "application/json"
            }
        };
    }

    private string? ExtractText(GeminiRawResponse? response) 
        => response?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

    private string ResolveApiKey() => Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? _settings.ApiKey;
}
