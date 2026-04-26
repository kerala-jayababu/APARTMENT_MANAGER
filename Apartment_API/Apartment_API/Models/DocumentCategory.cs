using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("DocumentCategories", Schema = "dbo")]
public sealed class DocumentCategory
{
    [Key]
    [Column("IdDocumentCategory")]
    public int IdDocumentCategory { get; set; }

    [Required, MaxLength(50)]
    public string CategoryCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string CategoryName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
