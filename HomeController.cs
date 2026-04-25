// HomeController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PROJECT_V1_SALES_PROPOSAL_GENERATOR.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Example of an updated method with improved error handling
        public async Task<IActionResult> GenerateProposal()
        {
            try
            {
                // Injected option lists
                var optionList = _configuration.GetSection("OptionLists").Get<YourOptionType>();
                // Success logging
                // Perform proposal generation logic...
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation appropriately
                return BadRequest("Proposal generation was canceled.");
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                return StatusCode(500, ex.Message);
            }
        }

        public IActionResult DownloadPdf(int documentId)
        {
            // Input validation
            if (documentId <= 0)
            {
                return BadRequest("Invalid document ID.");
            }
            try
            {
                // PDF download logic...
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}