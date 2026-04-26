using System.ComponentModel.DataAnnotations;

namespace Apartment_API.DTO;

public sealed class RegisterRequestDto
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(200), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Required, MinLength(6), MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}
