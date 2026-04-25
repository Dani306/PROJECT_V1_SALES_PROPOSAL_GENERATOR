#nullable enable

using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using PROJECT_V1.Models;
using PROJECT_V1.Services;

namespace PROJECT_V1.Controllers;

public class HomeController : Controller
{
    private static readonly List<string> IndustryOptions = new()
    {
        "SaaS",
        "Healthcare",
        "FinTech",
        "Retail",
        "Manufacturing",
        "Logistics",
        "Education",
        "Hospitality",
        "Real Estate",
        "Other"
    };

    private static readonly List<string> BudgetOptions = new()
    {
        "$10k - $25k",
        "$25k - $50k",
        "$50k - $100k",
        "$100k - $250k",
        "$250k+"
    };

    private static readonly List<string> ToneOptions = new()
    {
        "Professional",
        "Executive",
        "Startup"
    };

    private readonly IProposalService _proposalService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IProposalService proposalService, ILogger<HomeController> logger)
    {
        _proposalService = proposalService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var model = CreateViewModel();
        model.Request = GetDefaultRequest();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([Bind(Prefix = "Request")] ProposalRequest request, CancellationToken cancellationToken)
    {
        var model = CreateViewModel();
        model.Request = request ?? new ProposalRequest();

        if (!ModelState.IsValid)
        {
            model.ErrorMessage = "Please fill out all fields so we can build a strong proposal.";
            return View(model);
        }

        try
        {
            model.Result = await _proposalService.GenerateProposalAsync(model.Request, cancellationToken);
            _logger.LogInformation(
                "Proposal generated successfully for industry: {Industry}, budget: {Budget}",
                model.Request.Industry,
                model.Request.BudgetRange);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Proposal generation validation failed: {Message}", ex.Message);
            model.ErrorMessage = ex.Message;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Proposal generation request was cancelled");
            model.ErrorMessage = "Request timed out. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during proposal generation");
            model.ErrorMessage = "We couldn't generate the proposal right now. Please try again.";
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DownloadPdf(ProposalDocumentModel? document)
    {
        if (document == null || !IsValidDocument(document))
        {
            var model = CreateViewModel();
            model.Request = GetDefaultRequest();
            model.ErrorMessage = "Generate a proposal before downloading the PDF.";
            _logger.LogWarning("Download PDF attempt with invalid or empty document");
            return View("Index", model);
        }

        try
        {
            var pdfBytes = ProposalPdfRenderer.Render(document);
            var safeName = string.IsNullOrWhiteSpace(document.ClientName)
                ? "Proposal"
                : string.Concat(document.ClientName.Split(Path.GetInvalidFileNameChars()))
                    .Replace(' ', '_');
            var fileName = $"{safeName}_Proposal_{DateTime.UtcNow:yyyyMMdd}.pdf";

            _logger.LogInformation(
                "PDF generated successfully for client: {ClientName}",
                string.IsNullOrWhiteSpace(document.ClientName) ? "Unknown" : document.ClientName);

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF");
            var model = CreateViewModel();
            model.Request = GetDefaultRequest();
            model.ErrorMessage = "Failed to generate PDF. Please try again.";
            return View("Index", model);
        }
    }

    private static ProposalViewModel CreateViewModel()
    {
        return new ProposalViewModel
        {
            Industries = IndustryOptions,
            BudgetRanges = BudgetOptions,
            Tones = ToneOptions
        };
    }

    private static ProposalRequest GetDefaultRequest()
    {
        return new ProposalRequest();
    }

    private static bool IsValidDocument(ProposalDocumentModel document)
    {
        return !string.IsNullOrWhiteSpace(document.ExecutiveSummary) ||
               !string.IsNullOrWhiteSpace(document.ScopeOfWork) ||
               !string.IsNullOrWhiteSpace(document.Timeline) ||
               !string.IsNullOrWhiteSpace(document.PricingEstimate);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}