namespace Apartment_API.DTO;

public sealed class UserPublicDto
{
    public int IdUser { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public string? Designation { get; init; }
    public string? ProfilePhotoUrl { get; init; }
    public bool IsSuperAdmin { get; init; }
}
