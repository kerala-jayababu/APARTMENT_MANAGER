namespace Apartment_API.DTO;

public sealed class CurrentUserInfoDto
{
    public int IdUser { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool IsSuperAdmin { get; init; }
}
