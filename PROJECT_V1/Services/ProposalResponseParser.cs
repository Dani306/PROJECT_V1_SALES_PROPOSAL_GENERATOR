using System.Text.Json;
using PROJECT_V1.Models;

namespace PROJECT_V1.Services;

public static class ProposalResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ProposalResponse Parse(string json)
    {
        var proposal = JsonSerializer.Deserialize<ProposalResponse>(json, JsonOptions);
        if (proposal is null)
        {
            throw new InvalidOperationException("The AI response could not be parsed.");
        }

        proposal.Assumptions = NormalizeAssumptions(proposal.Assumptions, json);
        return proposal;
    }

    private static List<string> NormalizeAssumptions(List<string> assumptions, string json)
    {
        if (assumptions is { Count: > 0 })
        {
            return assumptions;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("assumptions", out var assumptionsElement))
            {
                if (assumptionsElement.ValueKind == JsonValueKind.String)
                {
                    var raw = assumptionsElement.GetString() ?? string.Empty;
                    return SplitAssumptions(raw);
                }

                if (assumptionsElement.ValueKind == JsonValueKind.Array)
                {
                    return assumptionsElement
                        .EnumerateArray()
                        .Select(item => item.GetString() ?? string.Empty)
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Select(item => item.Trim())
                        .ToList();
                }
            }
        }
        catch (JsonException)
        {
            return new List<string>();
        }

        return new List<string>();
    }

    private static List<string> SplitAssumptions(string raw)
    {
        return raw
            .Split(new[] { "\r\n", "\n", ";", "•" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim().TrimStart('-', '•', ' '))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }
}
