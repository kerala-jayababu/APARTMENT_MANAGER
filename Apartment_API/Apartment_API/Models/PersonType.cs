using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("PersonTypes", Schema = "dbo")]
public sealed class PersonType
{
    [Key]
    [Column("IdPersonType")]
    public int IdPersonType { get; set; }

    [Required, MaxLength(30)]
    public string PersonTypeCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string PersonTypeName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
