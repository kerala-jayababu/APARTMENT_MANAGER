using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("Reconciliations", Schema = "dbo")]
public sealed class Reconciliation
{
    [Key, Column("IdReconciliation")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdReconciliation { get; set; }

    public int ApartmentId { get; set; }
    public int FiscalYearId { get; set; }

    [MaxLength(10)]
    public string PeriodType { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime PeriodStartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime PeriodEndDate { get; set; }

    [Precision(14, 2)]
    public decimal TotalIncome { get; set; }

    [Precision(14, 2)]
    public decimal TotalExpense { get; set; }

    [Precision(15, 2)]
    public decimal? NetSurplus { get; set; }

    public int StatusId { get; set; }
    public int InitiatedBy { get; set; }
    public DateTime InitiatedAt { get; set; }
    public int? TreasurerApprovedBy { get; set; }
    public DateTime? TreasurerApprovedAt { get; set; }
    public int? SecretaryApprovedBy { get; set; }
    public DateTime? SecretaryApprovedAt { get; set; }
    public DateTime? LockedAt { get; set; }

    [MaxLength(1000)]
    public string? ReturnRemarks { get; set; }

    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ReconciliationRef { get; set; }
}
