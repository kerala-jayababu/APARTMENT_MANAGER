using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("Documents", Schema = "dbo")]
public sealed class StoredDocument
{
    [Key, Column("IdDocument")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdDocument { get; set; }

    public int ApartmentId { get; set; }
    public int CategoryId { get; set; }

    [Required, MaxLength(300)]
    public string DocumentName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? FileUrl { get; set; }
    public int? FileSizeKb { get; set; }

    [MaxLength(100)]
    public string? MimeType { get; set; }

    [MaxLength(30)]
    public string? LinkedEntityType { get; set; }
    public int? LinkedEntityId { get; set; }
    public int UploadedByUserId { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
