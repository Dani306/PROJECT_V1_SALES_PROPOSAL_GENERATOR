namespace PROJECT_V1.Models;

public class ProposalGeneratorOptions
{
    public const string SectionName = "ProposalGenerator";

    public double Temperature { get; set; } = 0.2;

    public int MaxOutputTokens { get; set; } = 2500;
}
