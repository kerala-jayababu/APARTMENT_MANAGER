using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("OwnershipTypes", Schema = "dbo")]
public sealed class OwnershipType
{
    [Key]
    [Column("IdOwnershipType")]
    public int IdOwnershipType { get; set; }

    [Required, MaxLength(30)]
    public string OwnershipCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string OwnershipName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
