using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("CommitteeRoles", Schema = "dbo")]
public sealed class CommitteeRole
{
    [Key]
    [Column("IdCommitteeRole")]
    public int IdCommitteeRole { get; set; }

    [Required, MaxLength(50)]
    public string RoleCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string RoleName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
