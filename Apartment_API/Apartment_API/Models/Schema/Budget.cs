using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("Budgets", Schema = "dbo")]
public sealed class Budget
{
    [Key, Column("IdBudget")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdBudget { get; set; }

    public int ApartmentId { get; set; }
    public int FiscalYearId { get; set; }
    public int ExpenseHeadId { get; set; }

    [Precision(14, 2)]
    public decimal BudgetAmount { get; set; }

    [Precision(14, 2)]
    public decimal ActualAmount { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
