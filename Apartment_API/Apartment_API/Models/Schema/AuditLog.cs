using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("AuditLog", Schema = "dbo")]
public sealed class AuditLog
{
    [Key, Column("IdAuditLog")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long IdAuditLog { get; set; }

    public int? ApartmentId { get; set; }
    public int? UserId { get; set; }

    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TableName { get; set; }
    public int? RecordId { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? OldValues { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? NewValues { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }
    public DateTime OccurredAt { get; set; }
}
