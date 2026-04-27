using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("MMCBatches", Schema = "dbo")]
public sealed class MmcBatch
{
    [Key, Column("IdMMCBatch")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdMmcBatch { get; set; }

    public int ApartmentId { get; set; }
    public int MmcPeriodId { get; set; }

    [MaxLength(20)]
    public string StatusCode { get; set; } = "PENDING";

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public int SubmittedByUserId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? RejectedByUserId { get; set; }
    public DateTime? RejectedAt { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
