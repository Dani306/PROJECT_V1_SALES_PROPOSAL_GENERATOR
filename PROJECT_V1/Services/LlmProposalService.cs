using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PROJECT_V1.Models;
using PROJECT_V1.Infrastructure;

namespace PROJECT_V1.Services;

public sealed class LlmProposalService(
    IServiceProvider serviceProvider,
    IOptions<LlmOptions> options) : IProposalService
{
    public async Task<Result<ProposalResponse>> GenerateProposalAsync(
        ProposalRequest request, 
        CancellationToken ct = default)
    {
        var providerKey = options.Value.Provider?.Trim();

        // Resolve the specific provider dynamically based on configuration
        var provider = serviceProvider.GetKeyedService<IProposalService>(providerKey);

        if (provider is null)
        {
            return Result.Failure($"LLM provider '{providerKey}' is not configured or supported.");
        }

        return await provider.GenerateProposalAsync(request, ct);
    }
}
