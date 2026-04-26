using System.ComponentModel.DataAnnotations;

namespace Apartment_API.DTO;

public sealed class VerifyOtpDto
{
    [Required, MaxLength(200), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(4), MaxLength(10)]
    public string Otp { get; set; } = string.Empty;
}
