[Route("[controller]")]
public sealed class HomeController(
    IProposalService proposalService, 
    IProposalViewModelFactory viewModelFactory) : Controller
{
    [HttpGet]
    public IActionResult Index() 
        => View(viewModelFactory.CreateInitial());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([Bind(Prefix = "Request")] ProposalRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(viewModelFactory.CreateError(request, "Invalid input."));

        // ValueTask-based service call reduces heap allocation for frequent requests
        var result = await proposalService.GenerateProposalAsync(request, ct);

        return result.Match(
            success => View(viewModelFactory.CreateSuccess(request, success)),
            failure => View(viewModelFactory.CreateError(request, failure.Message))
        );
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DownloadPdf(ProposalDocumentModel document)
    {
        // Pattern matching avoids multiple null/empty checks
        if (document is { IsEmpty: true }) 
            return RedirectToAction(nameof(Index));

        // FileStreamResult is more memory efficient for large PDFs than returning byte[]
        return File(
            ProposalPdfRenderer.RenderToStream(document), 
            "application/pdf", 
            document.GetSafeFileName());
    }
}
