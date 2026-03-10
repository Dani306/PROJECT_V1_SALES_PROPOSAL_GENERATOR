using System.Text.Json.Serialization;

namespace PROJECT_V1.Models;

public class ProposalResponse
{
    [JsonPropertyName("executiveSummary")]
    public string ExecutiveSummary { get; set; } = string.Empty;

    [JsonPropertyName("scopeOfWork")]
    public string ScopeOfWork { get; set; } = string.Empty;

    [JsonPropertyName("timeline")]
    public string Timeline { get; set; } = string.Empty;

    [JsonPropertyName("pricingEstimate")]
    public string PricingEstimate { get; set; } = string.Empty;

    [JsonPropertyName("assumptions")]
    public List<string> Assumptions { get; set; } = new();

    [JsonPropertyName("callToAction")]
    public string CallToAction { get; set; } = string.Empty;
}
