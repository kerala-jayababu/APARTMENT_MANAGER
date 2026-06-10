using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("Modules", Schema = "dbo")]
public sealed class AppModule
{
    [Key]
    [Column("IdModule")]
    public int IdModule { get; set; }

    [Required, MaxLength(50)]
    public string ModuleCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ModuleName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string ModuleGroup { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ParentModuleCode { get; set; }

    [MaxLength(300)]
    public string? Description { get; set; }

    [MaxLength(40)]
    public string? IconCode { get; set; }

    [MaxLength(100)]
    public string? RoutePath { get; set; }

    public bool SupportsView { get; set; }
    public bool SupportsCreate { get; set; }
    public bool SupportsEdit { get; set; }
    public bool SupportsDelete { get; set; }
    public bool SupportsApprove { get; set; }
    public bool SupportsExport { get; set; }

    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
