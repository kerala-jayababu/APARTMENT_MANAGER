using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IJwtTokenService
{
    (string accessToken, DateTime expiresAtUtc) CreateToken(UserPublicDto user);
}
