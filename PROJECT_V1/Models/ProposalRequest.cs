using System.ComponentModel.DataAnnotations;

namespace PROJECT_V1.Models;

public class ProposalRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string ClientName { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Industry { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Business Requirements cannot exceed 1000 characters.")]
    public string Requirements { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string BudgetRange { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    public string Tone { get; set; } = "Professional";
}
