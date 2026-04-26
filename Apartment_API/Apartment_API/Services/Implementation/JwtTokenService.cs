using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Apartment_API.Configuration;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Apartment_API.Services.Implementation;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _opt = options.Value;

    private void EnsureSecret()
    {
        if (string.IsNullOrWhiteSpace(_opt.SecretKey) || _opt.SecretKey.Length < 32)
            throw new InvalidOperationException("Jwt:SecretKey must be at least 32 characters.");
    }

    public (string token, DateTime expiresAtUtc) CreateApartmentSelectionToken(UserPublicDto user)
    {
        EnsureSecret();
        var minutes = Math.Max(5, _opt.ApartmentSelectionExpiresInMinutes);
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(minutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.IdUser.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypesExtra.Phone, user.PhoneNumber ?? string.Empty),
            new(ClaimTypesExtra.TokenPurpose, TokenPurposeValues.ApartmentSelect)
        };

        return (WriteToken(claims, expiresAtUtc), expiresAtUtc);
    }

    public (string accessToken, DateTime expiresAtUtc) CreateApartmentAccessToken(
        UserPublicDto user,
        ApartmentTokenContext? apartmentContext)
    {
        EnsureSecret();
        if (apartmentContext is null && !user.IsSuperAdmin)
            throw new InvalidOperationException("Apartment context is required unless the user is a SuperAdmin.");

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_opt.ExpiresInMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.IdUser.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.IsSuperAdmin ? "SuperAdmin" : "User"),
            new(ClaimTypesExtra.Phone, user.PhoneNumber ?? string.Empty),
            new(ClaimTypesExtra.TokenPurpose, TokenPurposeValues.ApiAccess)
        };

        if (apartmentContext is not null)
        {
            claims.Add(new Claim(ClaimTypesExtra.ApartmentId, apartmentContext.IdApartment.ToString()));
            claims.Add(new Claim(ClaimTypesExtra.ApartmentName, apartmentContext.ApartmentName));
            claims.Add(new Claim(ClaimTypesExtra.ApartmentUserRoleId, apartmentContext.RoleId.ToString()));
        }

        return (WriteToken(claims, expiresAtUtc), expiresAtUtc);
    }

    private string WriteToken(List<Claim> claims, DateTime expiresAtUtc)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
