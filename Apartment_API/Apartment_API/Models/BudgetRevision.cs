using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

/// <summary>Budget revision line (spec M09). Batch = rows sharing Apt + FY + CreatedBy + CreatedAt.</summary>
[Table("BudgetRevisions", Schema = "dbo")]
public sealed class BudgetRevision
{
    [Key, Column("IdBudgetRevision")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdBudgetRevision { get; set; }

    public int ApartmentId { get; set; }

    /// <summary>FK → Budgets.IdBudget</summary>
    public int BudgetId { get; set; }

    public int FiscalYearId { get; set; }

    [Precision(14, 2)]
    public decimal OriginalAmount { get; set; }

    [Precision(14, 2)]
    public decimal RevisedAmount { get; set; }

    [Required, MaxLength(500)]
    public string ReasonForRevision { get; set; } = string.Empty;

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    /// <summary>Draft / PendingL1 / PendingL2 / Active / Rejected — null treated as Draft.</summary>
    [MaxLength(50)]
    public string? ApprovalStatus { get; set; }

    /// <summary>Y once Active; N or null otherwise.</summary>
    [MaxLength(10)]
    public string? IsLocked { get; set; }

    /// <summary>Duplicated on each line in the batch for querying.</summary>
    [MaxLength(100)]
    public string? RevisionTitle { get; set; }

    [MaxLength(2000)]
    public string? OverallJustification { get; set; }

    [MaxLength(500)]
    public string? SupportingDocUrl { get; set; }

    public DateTime? L1ApprovedAt { get; set; }
    public int? L1ApprovedByUserId { get; set; }
    public DateTime? L2ApprovedAt { get; set; }
    public int? L2ApprovedByUserId { get; set; }

    public DateTime? RejectedAt { get; set; }
    public int? RejectedByUserId { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }
}
