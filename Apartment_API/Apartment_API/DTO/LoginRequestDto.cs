using System.ComponentModel.DataAnnotations;

namespace Apartment_API.DTO;

public sealed class LoginRequestDto
{
    [Required, MaxLength(200), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}
