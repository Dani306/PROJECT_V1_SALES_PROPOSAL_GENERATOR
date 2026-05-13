[Route("[controller]")]
public sealed class HomeController(
    IProposalService proposalService, 
    IProposalViewModelFactory viewModelFactory) : Controller
{
    [HttpGet]
    public IActionResult Index() 
        => View(viewModelFactory.Create());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([Bind(Prefix = "Request")] ProposalRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(viewModelFactory.Create(request, "Invalid input details."));

        // The service returns a Discriminated Union (Result)
        var outcome = await proposalService.GenerateAsync(request, ct);

        return outcome switch
        {
            { IsSuccess: true } => View(viewModelFactory.Create(request, outcome.Value)),
            _ => View(viewModelFactory.Create(request, outcome.Error))
        };
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DownloadPdf(ProposalDocumentModel document)
    {
        // Guard clause using the model's internal domain logic
        if (document.IsInvalid) return RedirectToAction(nameof(Index));

        return File(
            document.ToStream(), 
            "application/pdf", 
            document.SafeFileName);
    }
}
