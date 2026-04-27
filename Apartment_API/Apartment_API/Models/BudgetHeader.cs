using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("BudgetHeaders", Schema = "dbo")]
public sealed class BudgetHeader
{
    [Key, Column("IdBudgetHeader")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdBudgetHeader { get; set; }

    public int ApartmentId { get; set; }
    public int FiscalYearId { get; set; }

    [MaxLength(20)]
    public string StatusCode { get; set; } = "DRAFT";

    public int? SubmittedByUserId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }
    public int? RejectedByUserId { get; set; }
    public DateTime? RejectedAt { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
