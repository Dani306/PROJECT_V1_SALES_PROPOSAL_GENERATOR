using PROJECT_V1.Models;

namespace PROJECT_V1.Services;

public static class ProposalPromptBuilder
{
    public static (string SystemPrompt, string UserPrompt) BuildPrompt(ProposalRequest request)
    {
        var systemPrompt = "You are a senior enterprise sales consultant creating persuasive, concise sales proposals. " +
                           "Return only valid JSON that matches the required schema. " +
                           "Do not include markdown, code fences, or extra commentary. " +
                           "Keep each section tight and business-ready.";

        var userPrompt = $"Client Name: {request.ClientName}\n" +
                         $"Industry: {request.Industry}\n" +
                         $"Budget Range: {request.BudgetRange}\n" +
                         $"Proposal Tone: {request.Tone}\n" +
                         $"Business Requirements: {request.Requirements}\n\n" +
                         "Generate a multi-section sales proposal with the exact keys:\n" +
                         "executiveSummary, scopeOfWork, timeline, pricingEstimate, assumptions (array of short bullet phrases), callToAction.";

        return (systemPrompt, userPrompt);
    }
}
