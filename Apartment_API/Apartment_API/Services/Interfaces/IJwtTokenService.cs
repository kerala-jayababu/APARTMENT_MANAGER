using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IJwtTokenService
{
    /// <summary>Short-lived JWT to call apartment list and tenant-access only (not for business APIs).</summary>
    (string token, DateTime expiresAtUtc) CreateApartmentSelectionToken(UserPublicDto user);

    /// <summary>API access token. If <paramref name="apartmentContext" /> is null, user must be SuperAdmin.</summary>
    (string accessToken, DateTime expiresAtUtc) CreateApartmentAccessToken(
        UserPublicDto user,
        ApartmentTokenContext? apartmentContext);
}
