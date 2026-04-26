using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface ICommitteeMemberService
{
    Task<IReadOnlyList<CommitteeMemberListDto>> GetMembersForTenureAsync(
        int apartmentId, int tenureId, CancellationToken cancellationToken = default);
    Task<PagedResult<CommitteeMemberListDto>> ListMembersAsync(
        int apartmentId, int? committeeTenureId, int? committeeRoleId, string? statusCode, string? search, int page, int pageSize,
        CancellationToken cancellationToken = default);
    Task<CommitteeMemberListDto?> GetMemberByIdAsync(int apartmentId, int id, CancellationToken cancellationToken = default);
    Task<int> AssignMemberAsync(int apartmentId, int userId, AssignCommitteeMemberRequest request, CancellationToken cancellationToken = default);
    Task UpdateMemberAsync(
        int apartmentId, int userId, int id, UpdateCommitteeMemberRequest request, CancellationToken cancellationToken = default);
    Task EndMemberAsync(
        int apartmentId, int userId, int id, EndCommitteeMemberRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EligibleOwnerDto>> GetEligibleOwnersAsync(
        int apartmentId, int committeeTenureId, int? committeeRoleId, CancellationToken cancellationToken = default);
}
