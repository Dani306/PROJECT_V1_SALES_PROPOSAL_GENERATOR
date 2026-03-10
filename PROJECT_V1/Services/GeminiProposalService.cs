using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PROJECT_V1.Models;

namespace PROJECT_V1.Services;

public class GeminiProposalService : IProposalService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ProposalGeneratorOptions _proposalOptions;
    private readonly ILogger<GeminiProposalService> _logger;

    public GeminiProposalService(
        IHttpClientFactory httpClientFactory,
        IOptions<LlmOptions> options,
        IOptions<ProposalGeneratorOptions> proposalOptions,
        ILogger<GeminiProposalService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Gemini");
        _settings = options.Value.Gemini;
        _proposalOptions = proposalOptions.Value;
        _logger = logger;
    }

    public async Task<ProposalResponse> GenerateProposalAsync(ProposalRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Missing Gemini API key. Set GEMINI_API_KEY or Llm:Gemini:ApiKey in appsettings.json.");
        }

        var (systemPrompt, userPrompt) = ProposalPromptBuilder.BuildPrompt(request);
        var combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = combinedPrompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = _proposalOptions.Temperature,
                maxOutputTokens = _proposalOptions.MaxOutputTokens,
                responseMimeType = "application/json"
            }
        };

        var maxRetries = Math.Max(0, _settings.MaxRetries);
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var baseUrl = _settings.ApiBase.TrimEnd('/') + "/";
                var endpoint = $"{baseUrl}{_settings.Model}:generateContent";
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
                httpRequest.Headers.Add("x-goog-api-key", apiKey);
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini API failure: {Status} {Body}", response.StatusCode, responseContent);
                    throw new InvalidOperationException("The AI service returned an error. Please verify your API key and try again.");
                }

                var proposalJson = ExtractProposalJson(responseContent);
                return ProposalResponseParser.Parse(proposalJson);
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Gemini attempt {Attempt} failed. Retrying...", attempt + 1);
                await Task.Delay(250 * (attempt + 1), cancellationToken);
            }
        }

        throw new InvalidOperationException("The AI service failed after multiple retries. Please try again.");
    }

    private string ResolveApiKey()
    {
        var envKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        return string.IsNullOrWhiteSpace(envKey) ? _settings.ApiKey : envKey;
    }

    private static string ExtractProposalJson(string rawResponse)
    {
        using var doc = JsonDocument.Parse(rawResponse);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("The AI response was missing candidates.");
        }

        var candidate = candidates[0];
        if (!candidate.TryGetProperty("content", out var content))
        {
            throw new InvalidOperationException("The AI response was missing content.");
        }

        if (!content.TryGetProperty("parts", out var parts))
        {
            throw new InvalidOperationException("The AI response was missing parts.");
        }

        var builder = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var text))
            {
                builder.Append(text.GetString());
            }
        }

        var payload = builder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new InvalidOperationException("The AI response was empty.");
        }

        return ExtractJsonBlock(payload);
    }

    private static string ExtractJsonBlock(string payload)
    {
        payload = payload.Trim();
        if (payload.StartsWith("{") && payload.EndsWith("}"))
        {
            return payload;
        }

        var firstBrace = payload.IndexOf('{');
        var lastBrace = payload.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            return payload.Substring(firstBrace, lastBrace - firstBrace + 1);
        }

        return payload;
    }
}
