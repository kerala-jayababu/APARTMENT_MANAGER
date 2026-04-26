using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("AmenityTypes", Schema = "dbo")]
public sealed class AmenityType
{
    [Key]
    [Column("IdAmenityType")]
    public int IdAmenityType { get; set; }

    [Required, MaxLength(50)]
    public string AmenityTypeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string AmenityTypeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
