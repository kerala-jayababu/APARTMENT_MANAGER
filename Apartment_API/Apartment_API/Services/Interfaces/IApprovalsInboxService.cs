using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IApprovalsInboxService
{
    Task<ApprovalsInboxSummaryDto> GetSummaryAsync(int apartmentId, CancellationToken cancellationToken = default);

    /// <param name="kindFilter">all | set_mmc | budget | amenity_booking (budget includes header + revision).</param>
    Task<PagedResult<ApprovalInboxItemDto>> ListPendingAsync(
        int apartmentId,
        string? kindFilter,
        DateOnly? submittedFrom,
        DateOnly? submittedTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
