namespace PROJECT_V1.Services;

public sealed class LlmProposalService(
    IEnumerable<IProposalProvider> providers,
    IOptions<LlmOptions> options) : IProposalService
{
    private readonly LlmOptions _options = options.Value;

    public async Task<Result<ProposalResponse>> GenerateProposalAsync(
        ProposalRequest request, 
        CancellationToken ct = default)
    {
        var providerName = _options.Provider ?? "Gemini";

        // Strategy Pattern: Find the service that matches the configured name
        var service = providers.FirstOrDefault(p => 
            p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

        if (service is null)
        {
            return Result<ProposalResponse>.Failure(
                $"LLM provider '{providerName}' is not registered or supported.");
        }

        return await service.GenerateProposalAsync(request, ct);
    }
}
