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

    public (string accessToken, DateTime expiresAtUtc) CreateToken(UserPublicDto user)
    {
        if (string.IsNullOrWhiteSpace(_opt.SecretKey) || _opt.SecretKey.Length < 32)
            throw new InvalidOperationException("Jwt:SecretKey must be at least 32 characters.");

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_opt.ExpiresInMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.IdUser.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.IsSuperAdmin ? "SuperAdmin" : "User"),
            new(ClaimTypesExtra.Phone, user.PhoneNumber ?? string.Empty)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return (accessToken, expiresAtUtc);
    }
}
