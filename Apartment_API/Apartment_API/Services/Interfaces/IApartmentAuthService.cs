using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IApartmentAuthService
{
    Task<IReadOnlyList<AvailableApartmentItemDto>> GetApartmentsForUserAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues the API access JWT. Fails if the user has no active <c>ApartmentUsers</c> row at all,
    /// or no mapping for <paramref name="apartmentId" />.
    /// </summary>
    Task<(TenantTokenResponseDto? data, string? errorCode)> CreateTenantAccessTokenAsync(
        int userId,
        int apartmentId,
        CancellationToken cancellationToken = default);
}
