using Microsoft.AspNetCore.Mvc;
using PROJECT_V1.Models;
using PROJECT_V1.Services;
using PROJECT_V1.Infrastructure; // Assuming Result<T> lives here

namespace PROJECT_V1.Controllers;

[Route("[controller]")]
public sealed class HomeController(
    IProposalService proposalService,
    IProposalViewModelFactory viewModelFactory,
    ILogger<HomeController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index() 
        => View(viewModelFactory.CreateInitial());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([Bind(Prefix = "Request")] ProposalRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(viewModelFactory.CreateError(request, "Please fill out all required fields."));

        // Use the Result pattern instead of try-catch for better performance
        var result = await proposalService.GenerateProposalAsync(request, ct);

        return result.Match<IActionResult>(
            success => View(viewModelFactory.CreateSuccess(request, success)),
            failure => {
                logger.LogWarning("Proposal failed: {Message}", failure);
                return View(viewModelFactory.CreateError(request, failure));
            }
        );
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DownloadPdf(ProposalDocumentModel document)
    {
        if (document is { IsEmpty: true })
        {
            return RedirectToAction(nameof(Index), new { error = "Generate a proposal first." });
        }

        // Return a FileStreamResult to avoid loading the entire byte array into memory
        var pdfStream = ProposalPdfRenderer.RenderToStream(document);
        return File(pdfStream, "application/pdf", document.GenerateFileName());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
