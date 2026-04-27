using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("BudgetRevisions", Schema = "dbo")]
public sealed class BudgetRevision
{
    [Key, Column("IdBudgetRevision")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdBudgetRevision { get; set; }

    public int ApartmentId { get; set; }
    public int FiscalYearId { get; set; }
    public int ExpenseHeadId { get; set; }

    [Precision(14, 2)]
    public decimal OriginalBudget { get; set; }

    [Precision(14, 2)]
    public decimal RevisedBudget { get; set; }

    [MaxLength(1000)]
    public string Justification { get; set; } = string.Empty;

    public int? SupportingDocId { get; set; }

    [MaxLength(20)]
    public string StatusCode { get; set; } = "PENDING";

    public int SubmittedByUserId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? RejectedByUserId { get; set; }
    public DateTime? RejectedAt { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }
}
