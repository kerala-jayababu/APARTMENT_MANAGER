using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("UnitTypes", Schema = "dbo")]
public sealed class UnitType
{
    [Key]
    [Column("IdUnitType")]
    public int IdUnitType { get; set; }

    [Required, MaxLength(30)]
    public string UnitTypeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string UnitTypeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
