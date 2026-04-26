using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("UnitStatuses", Schema = "dbo")]
public sealed class UnitStatus
{
    [Key]
    [Column("IdUnitStatus")]
    public int IdUnitStatus { get; set; }

    [Required, MaxLength(30)]
    public string StatusCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string StatusName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
