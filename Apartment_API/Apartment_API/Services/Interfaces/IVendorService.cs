using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IVendorService
{
    Task<IReadOnlyList<VendorDto>> ListVendorsForApartmentAsync(int apartmentId, CancellationToken cancellationToken = default);

    /// <summary>Resolves by id when the vendor belongs to <paramref name="apartmentId" />.</summary>
    Task<VendorDto?> GetByIdAsync(int idVendor, int apartmentId, CancellationToken cancellationToken = default);

    /// <summary>Insert when <c>IdVendor &lt;= 0</c>; else update. Scoped to <paramref name="apartmentId" /> (from the access token).</summary>
    Task<EntitySaveResult<VendorDto>?> SaveAsync(
        VendorSaveDto dto,
        int userId,
        int apartmentId,
        CancellationToken cancellationToken = default);
}
