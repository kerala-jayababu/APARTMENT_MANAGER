using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IApprovalRuleService
{
    Task<IReadOnlyList<ApprovalRuleDto>> ListAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, UpdateApprovalRuleRequest request, CancellationToken cancellationToken = default);
}
