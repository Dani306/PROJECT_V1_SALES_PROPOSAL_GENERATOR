using PROJECT_V1.Models;

namespace PROJECT_V1.Services;

public interface IProposalService
{
    Task<ProposalResponse> GenerateProposalAsync(ProposalRequest request, CancellationToken cancellationToken = default);
}
