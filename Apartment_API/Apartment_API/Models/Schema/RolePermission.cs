using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("RolePermissions", Schema = "dbo")]
public sealed class RolePermission
{
    [Key, Column("IdRolePermission")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdRolePermission { get; set; }

    public int ApartmentId { get; set; }
    public int RoleId { get; set; }

    [Required, MaxLength(50)]
    public string ModuleCode { get; set; } = string.Empty;

    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
