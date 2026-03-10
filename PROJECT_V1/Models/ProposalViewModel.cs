namespace PROJECT_V1.Models;

public class ProposalViewModel
{
    public ProposalRequest Request { get; set; } = new();

    public ProposalResponse? Result { get; set; }

    public string? ErrorMessage { get; set; }

    public List<string> Industries { get; set; } = new();

    public List<string> BudgetRanges { get; set; } = new();

    public List<string> Tones { get; set; } = new();
}
