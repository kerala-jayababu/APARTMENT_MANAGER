using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("VendorTypes", Schema = "dbo")]
public sealed class VendorType
{
    [Key]
    [Column("IdVendorType")]
    public int IdVendorType { get; set; }

    [Required, MaxLength(50)]
    public string VendorTypeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string VendorTypeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
