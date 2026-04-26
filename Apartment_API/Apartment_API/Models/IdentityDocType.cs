using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("IdentityDocTypes", Schema = "dbo")]
public sealed class IdentityDocType
{
    [Key]
    [Column("IdIdentityDocType")]
    public int IdIdentityDocType { get; set; }

    [Required, MaxLength(30)]
    public string DocTypeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string DocTypeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
