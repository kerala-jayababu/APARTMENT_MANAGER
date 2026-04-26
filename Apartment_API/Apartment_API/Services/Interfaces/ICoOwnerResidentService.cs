using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface ICoOwnerResidentService
{
    Task<PagedResult<CoOwnerListDto>> ListAsync(
        int apartmentId, int? primaryOwnerPersonId, int? unitId, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<OwnerDetailDto?> GetByUnitOwnerIdAsync(int apartmentId, int idUnitOwner, CancellationToken cancellationToken = default);
    Task<CoOwnerCreatedDto> CreateAsync(
        int apartmentId, int userId, CreateCoOwnerRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(
        int apartmentId, int userId, int idUnitOwner, CreateCoOwnerRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int apartmentId, int idUnitOwner, int userId, CancellationToken cancellationToken = default);
}
