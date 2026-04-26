using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("Users", Schema = "dbo")]
[Index(nameof(Email), IsUnique = true)]
public class User
{
    [Key]
    [Column("IdUser")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdUser { get; set; }

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsSuperAdmin { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public string? Designation { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }

    public string? RefreshToken { get; set; }

    [MaxLength(10)]
    public string? LoginOTP { get; set; }

    public DateTime? OTPCreatedDate { get; set; }

    public DateTime? OTPExpiryDate { get; set; }
}
