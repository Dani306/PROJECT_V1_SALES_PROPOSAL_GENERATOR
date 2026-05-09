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
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Proposal generation failed.");
            model.ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Proposal generation failed unexpectedly.");
            model.ErrorMessage = "We couldn't generate the proposal right now. Please try again.";
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DownloadPdf(ProposalDocumentModel document)
    {
        if (string.IsNullOrWhiteSpace(document.ExecutiveSummary) &&
            string.IsNullOrWhiteSpace(document.ScopeOfWork) &&
            string.IsNullOrWhiteSpace(document.Timeline) &&
            string.IsNullOrWhiteSpace(document.PricingEstimate))
        {
            var model = CreateViewModel();
            model.Request = GetDefaultRequest();
            model.ErrorMessage = "Generate a proposal before downloading the PDF.";
            return View("Index", model);
        }

        var pdfBytes = ProposalPdfRenderer.Render(document);
        var safeName = string.IsNullOrWhiteSpace(document.ClientName)
            ? "Proposal"
            : string.Concat(document.ClientName.Split(Path.GetInvalidFileNameChars()))
                .Replace(' ', '_');
        var fileName = $"{safeName}_Proposal_{DateTime.UtcNow:yyyyMMdd}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
