using System.ComponentModel.DataAnnotations;

namespace Apartment_API.DTO;

/// <summary>Returned from password login and OTP verify. Not the API access token—use <see cref="ApartmentSelectionToken" /> only for apartment list and tenant token.</summary>
public sealed class LoginAuthenticationResultDto
{
    public int IdUser { get; init; }
    public string ApartmentSelectionToken { get; init; } = string.Empty;
    public DateTime ApartmentSelectionExpiresAtUtc { get; init; }
}

/// <summary>Tenant context embedded in the access JWT (after apartment is chosen).</summary>
public sealed class ApartmentTokenContext
{
    public int IdApartment { get; init; }
    public string ApartmentName { get; init; } = string.Empty;
    public int RoleId { get; init; }
}

public sealed class AvailableApartmentItemDto
{
    public int IdApartment { get; init; }
    public string ApartmentCode { get; init; } = string.Empty;
    public string ApartmentName { get; init; } = string.Empty;
    public string AssociationName { get; init; } = string.Empty;
    public int RoleId { get; init; }
    public int IdApartmentUser { get; init; }
}

public sealed class TenantTokenRequestDto
{
    [Range(1, int.MaxValue)]
    public int ApartmentId { get; set; }
}

public sealed class TenantTokenResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public DateTime ExpiresAtUtc { get; init; }
    public int IdUser { get; init; }
    public int IdApartment { get; init; }
    public string ApartmentName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string FullName { get; init; } = string.Empty;
}
