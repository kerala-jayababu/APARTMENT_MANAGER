using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("Notices", Schema = "dbo")]
public sealed class Notice
{
    [Key, Column("IdNotice")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdNotice { get; set; }

    public int ApartmentId { get; set; }
    public int CategoryId { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string Body { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime PublishDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    [MaxLength(20)]
    public string Visibility { get; set; } = string.Empty;

    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int? PublishedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
