using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("NoticeCategories", Schema = "dbo")]
public sealed class NoticeCategory
{
    [Key]
    [Column("IdNoticeCategory")]
    public int IdNoticeCategory { get; set; }

    [Required, MaxLength(30)]
    public string CategoryCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; }
}
