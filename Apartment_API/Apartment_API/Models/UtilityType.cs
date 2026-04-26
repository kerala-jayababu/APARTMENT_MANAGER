using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("UtilityTypes", Schema = "dbo")]
public sealed class UtilityType
{
    [Key]
    [Column("IdUtilityType")]
    public int IdUtilityType { get; set; }

    [Required, MaxLength(30)]
    public string UtilityTypeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string UtilityTypeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
