using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("Roles", Schema = "dbo")]
public sealed class AppRole
{
    [Key]
    [Column("IdRole")]
    public int IdRole { get; set; }

    [Required, MaxLength(50)]
    public string RoleCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
