using Apartment_API.DTO;
using Microsoft.AspNetCore.Http;

namespace Apartment_API.Services.Interfaces;

public interface IHelpdeskService
{
    Task<HelpDeskCategoryListDto> ListCategoriesAsync(
        int apartmentId, string? q, bool? activeOnly, CancellationToken cancellationToken = default);

    Task<HelpDeskCategoryDto?> GetCategoryAsync(int apartmentId, int id, CancellationToken cancellationToken = default);

    Task<int> CreateCategoryAsync(int apartmentId, CreateHelpDeskCategoryRequest request, CancellationToken cancellationToken = default);

    Task UpdateCategoryAsync(int apartmentId, int id, UpdateHelpDeskCategoryRequest request, CancellationToken cancellationToken = default);

    Task DeleteCategoryAsync(int apartmentId, int id, CancellationToken cancellationToken = default);

    Task<LogComplaintResponseDto> LogComplaintAsync(
        int apartmentId,
        int userId,
        LogComplaintRequest request,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ComplaintListItemDto>> ListComplaintsAsync(
        int apartmentId,
        int userId,
        bool isAdmin,
        int? categoryId,
        string? status,
        string? priority,
        int? unitId,
        int? ownerTenantId,
        DateOnly? from,
        DateOnly? to,
        string? q,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ComplaintDetailDto?> GetComplaintAsync(
        int apartmentId,
        int userId,
        bool isAdmin,
        int id,
        bool includeHistory,
        CancellationToken cancellationToken = default);

    Task<AppendComplaintStatusResponseDto> AppendStatusAsync(
        int apartmentId,
        int userId,
        int complaintId,
        AppendComplaintStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HelpdeskStatusEntryDto>> GetStatusTimelineAsync(
        int apartmentId,
        int userId,
        bool isAdmin,
        int complaintId,
        CancellationToken cancellationToken = default);

    Task<HelpdeskFileUploadDto> UploadFileAsync(
        int apartmentId,
        int userId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<HelpdeskStatsDto> GetStatsAsync(
        int apartmentId,
        int? month,
        int? year,
        CancellationToken cancellationToken = default);

    Task<StatusSummaryReportDto> GetStatusSummaryReportAsync(
        int apartmentId,
        DateOnly from,
        DateOnly to,
        int? categoryId,
        string? format,
        CancellationToken cancellationToken = default);

    Task<CategoryBreakdownReportDto> GetCategoryBreakdownReportAsync(
        int apartmentId,
        DateOnly from,
        DateOnly to,
        int? categoryId,
        string? format,
        CancellationToken cancellationToken = default);

    Task<AgingReportDto> GetAgingReportAsync(
        int apartmentId,
        DateOnly? asOf,
        string? bucketsCsv,
        string? format,
        CancellationToken cancellationToken = default);

    Task<bool> IsHelpdeskAdminAsync(int? roleId, CancellationToken cancellationToken = default);
}
