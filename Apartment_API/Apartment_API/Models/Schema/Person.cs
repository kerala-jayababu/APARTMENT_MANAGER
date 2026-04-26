using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("Persons", Schema = "dbo")]
public sealed class Person
{
    [Key, Column("IdPerson")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdPerson { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(20)]
    public string PersonNumber { get; set; } = string.Empty;

    public int PersonTypeId { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? AlternatePhone { get; set; }
    public int? IdentityDocTypeId { get; set; }

    [MaxLength(100)]
    public string? IdentityDocNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? PanNumber { get; set; }
    public int? LinkedUserId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? EmergencyContactName { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? EmergencyContactPhone { get; set; }
    public int? ParentOwnerId { get; set; }
}
