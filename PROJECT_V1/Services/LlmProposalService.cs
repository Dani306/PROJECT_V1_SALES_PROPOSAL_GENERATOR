using Microsoft.Extensions.Options;
using PROJECT_V1.Models;

namespace PROJECT_V1.Services;

public class LlmProposalService : IProposalService
{
    private readonly IProposalService _openAiService;
    private readonly IProposalService _geminiService;
    private readonly LlmOptions _options;

    public LlmProposalService(
        OpenAiProposalService openAiService,
        GeminiProposalService geminiService,
        IOptions<LlmOptions> options)
    {
        _openAiService = openAiService;
        _geminiService = geminiService;
        _options = options.Value;
    }

    public Task<ProposalResponse> GenerateProposalAsync(ProposalRequest request, CancellationToken cancellationToken = default)
    {
        var provider = (_options.Provider ?? string.Empty).Trim();
        if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            return _geminiService.GenerateProposalAsync(request, cancellationToken);
        }

        if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            return _openAiService.GenerateProposalAsync(request, cancellationToken);
        }

        throw new InvalidOperationException($"Unknown LLM provider '{_options.Provider}'. Use 'Gemini' or 'OpenAI'.");
    }
}
