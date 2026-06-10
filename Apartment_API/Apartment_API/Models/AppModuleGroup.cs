using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("modulegroups", Schema = "dbo")]
public sealed class AppModuleGroup
{
    [Key]
    [Column("ModuleGroupCode")]
    [MaxLength(10)]
    public string ModuleGroupCode { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ModuleGroupName { get; set; }

    public int? DisplayOrder { get; set; }

    [MaxLength(1)]
    public string? IsActive { get; set; }

    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }
}
