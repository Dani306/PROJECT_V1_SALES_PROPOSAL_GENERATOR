using Microsoft.AspNetCore.Mvc;
using PROJECT_V1.Models;
using PROJECT_V1.Abstractions; // Contains IResult and Success/Failure types

namespace PROJECT_V1.Controllers;

[Route("[controller]")]
public class HomeController : Controller
{
    private readonly IProposalService _proposalService;
    private readonly IProposalViewModelFactory _viewModelFactory;

    public HomeController(
        IProposalService proposalService, 
        IProposalViewModelFactory viewModelFactory)
    {
        _proposalService = proposalService;
        _viewModelFactory = viewModelFactory;
    }

    [HttpGet]
    public IActionResult Index() 
        => View(_viewModelFactory.Create());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([Bind(Prefix = "Request")] ProposalRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(_viewModelFactory.Create(request, "Please correct the errors below."));

        // Using a Result pattern instead of raw try-catch
        var result = await _proposalService.GenerateProposalAsync(request, ct);

        return result.Match(
            success => View(_viewModelFactory.Create(request, success)),
            failure => View(_viewModelFactory.Create(request, failure.Message))
        );
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DownloadPdf(ProposalDocumentModel document)
    {
        return document switch
        {
            { IsEmpty: true } => RedirectToAction(nameof(Index)),
            _ => File(
                ProposalPdfRenderer.Render(document), 
                "application/pdf", 
                document.FileName)
        };
    }
}
