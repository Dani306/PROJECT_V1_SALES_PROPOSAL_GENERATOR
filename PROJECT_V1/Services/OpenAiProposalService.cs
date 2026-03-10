using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PROJECT_V1.Models;

namespace PROJECT_V1.Services;

public class OpenAiProposalService : IProposalService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiSettings _settings;
    private readonly ProposalGeneratorOptions _proposalOptions;
    private readonly ILogger<OpenAiProposalService> _logger;

    public OpenAiProposalService(
        IHttpClientFactory httpClientFactory,
        IOptions<LlmOptions> options,
        IOptions<ProposalGeneratorOptions> proposalOptions,
        ILogger<OpenAiProposalService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("OpenAI");
        _settings = options.Value.OpenAI;
        _proposalOptions = proposalOptions.Value;
        _logger = logger;
    }

    public async Task<ProposalResponse> GenerateProposalAsync(ProposalRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Missing OpenAI API key. Set OPENAI_API_KEY or Llm:OpenAI:ApiKey in appsettings.json.");
        }

        var (systemPrompt, userPrompt) = ProposalPromptBuilder.BuildPrompt(request);
        var payload = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = _proposalOptions.Temperature,
            max_tokens = _proposalOptions.MaxOutputTokens,
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "sales_proposal",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            executiveSummary = new { type = "string" },
                            scopeOfWork = new { type = "string" },
                            timeline = new { type = "string" },
                            pricingEstimate = new { type = "string" },
                            assumptions = new { type = "array", items = new { type = "string" } },
                            callToAction = new { type = "string" }
                        },
                        required = new[]
                        {
                            "executiveSummary",
                            "scopeOfWork",
                            "timeline",
                            "pricingEstimate",
                            "assumptions",
                            "callToAction"
                        },
                        additionalProperties = false
                    }
                }
            }
        };

        var maxRetries = Math.Max(0, _settings.MaxRetries);
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.ChatEndpoint);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenAI API failure: {Status} {Body}", response.StatusCode, responseContent);
                    throw new InvalidOperationException("The AI service returned an error. Please verify your API key and try again.");
                }

                var proposalJson = ExtractProposalJson(responseContent);
                return ProposalResponseParser.Parse(proposalJson);
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "OpenAI attempt {Attempt} failed. Retrying...", attempt + 1);
                await Task.Delay(250 * (attempt + 1), cancellationToken);
            }
        }

        throw new InvalidOperationException("The AI service failed after multiple retries. Please try again.");
    }

    private string ResolveApiKey()
    {
        var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        return string.IsNullOrWhiteSpace(envKey) ? _settings.ApiKey : envKey;
    }

    private static string ExtractProposalJson(string rawResponse)
    {
        using var doc = JsonDocument.Parse(rawResponse);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("The AI response was missing choices.");
        }

        var message = choices[0].GetProperty("message");
        var content = message.GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("The AI response was empty.");
        }

        return content;
    }

}
