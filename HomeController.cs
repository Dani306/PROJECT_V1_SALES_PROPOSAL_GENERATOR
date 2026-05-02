[Route("[controller]")]
public class HomeController : Controller
{
    private readonly IProposalService _proposalService;
    private readonly IProposalViewModelService _viewModelService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IProposalService proposalService, 
        IProposalViewModelService viewModelService,
        ILogger<HomeController> logger)
    {
        _proposalService = proposalService;
        _viewModelService = viewModelService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index() 
        => View(_viewModelService.BuildInitialModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([Bind(Prefix = "Request")] ProposalRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(_viewModelService.BuildErrorModel(request, "Please fill out all required fields."));

        try
        {
            var result = await _proposalService.GenerateProposalAsync(request, ct);
            return View(_viewModelService.BuildSuccessModel(request, result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Proposal generation failed for {Client}", request.ClientName);
            return View(_viewModelService.BuildErrorModel(request, "An unexpected error occurred. Please try again."));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DownloadPdf(ProposalDocumentModel document)
    {
        if (document.IsMissingContent())
            return RedirectToAction(nameof(Index));

        var pdfBytes = ProposalPdfRenderer.Render(document);
        return File(pdfBytes, "application/pdf", document.GenerateFileName());
    }
}
