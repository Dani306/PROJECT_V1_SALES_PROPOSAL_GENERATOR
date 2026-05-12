using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PROJECT_V1.Models;
using PROJECT_V1.Services;
using PROJECT_V1.Constants; // Hypothetical namespace for static data

namespace PROJECT_V1.Controllers;

public class HomeController : Controller
{
    private readonly IProposalService _proposalService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IProposalService proposalService, ILogger<HomeController> logger)
    {
        _proposalService = proposalService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index() 
        => View(CreateViewModel(new ProposalRequest()));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([Bind(Prefix = "Request")] ProposalRequest request, CancellationToken ct)
    {
        var model = CreateViewModel(request ?? new());

        if (!ModelState.IsValid)
        {
            model.ErrorMessage = "Please fill out all fields so we can build a strong proposal.";
            return View(model);
        }

        try
        {
            model.Result = await _proposalService.GenerateProposalAsync(request!, ct);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business logic validation failed for proposal.");
            model.ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during proposal generation for {Client}", request?.ClientName);
            model.ErrorMessage = "We couldn't generate the proposal right now. Please try again.";
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DownloadPdf(ProposalDocumentModel document)
    {
        if (document.IsEmpty()) // Encapsulated logic in the Model
        {
            var model = CreateViewModel(new());
            model.ErrorMessage = "Generate a proposal before downloading the PDF.";
            return View("Index", model);
        }

        var pdfBytes = ProposalPdfRenderer.Render(document);
        return File(pdfBytes, "application/pdf", GetSafeFileName(document.ClientName));
    }

    private static ProposalViewModel CreateViewModel(ProposalRequest request) => new()
    {
        Request = request,
        Industries = StaticData.IndustryOptions, // Moved to a static class/config
        BudgetRanges = StaticData.BudgetOptions,
        Tones = StaticData.ToneOptions
    };

    private static string GetSafeFileName(string? clientName)
    {
        var baseName = string.IsNullOrWhiteSpace(clientName) ? "Proposal" : clientName;
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = string.Join("_", baseName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        
        return $"{safeName.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.pdf";
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel 
    { 
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
    });
}
