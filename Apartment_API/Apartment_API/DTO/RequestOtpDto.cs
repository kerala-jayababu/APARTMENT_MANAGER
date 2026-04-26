using System.ComponentModel.DataAnnotations;

namespace Apartment_API.DTO;

public sealed class RequestOtpDto
{
    [Required, MaxLength(200), EmailAddress]
    public string Email { get; set; } = string.Empty;
}
