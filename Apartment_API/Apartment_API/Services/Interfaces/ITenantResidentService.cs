using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface ITenantResidentService
{
    Task<PagedResult<TenantListDto>> ListAsync(
        int apartmentId, string? search, int? unitId, bool? isActive, int? expiringWithinDays, int page, int pageSize,
        CancellationToken cancellationToken = default);
    Task<TenantListDto?> GetByAssignmentIdAsync(int apartmentId, int idTenantAssignment, CancellationToken cancellationToken = default);
    Task<TenantCreatedDto> CreateAsync(
        int apartmentId, int userId, CreateTenantRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(
        int apartmentId, int userId, int idTenantAssignment, CreateTenantRequest request, CancellationToken cancellationToken = default);
    Task VacateAsync(
        int apartmentId, int userId, int idTenantAssignment, VacateTenantRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantListDto>> GetExpiringAsync(
        int apartmentId, int withinDays, CancellationToken cancellationToken = default);
    Task<IdProofResultDto> UploadLeaseDocumentAsync(
        int apartmentId, int userId, int idTenantAssignment, Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}
