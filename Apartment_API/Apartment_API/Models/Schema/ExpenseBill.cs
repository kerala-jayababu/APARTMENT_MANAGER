using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("ExpenseBills", Schema = "dbo")]
public sealed class ExpenseBill
{
    [Key, Column("IdBill")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long IdBill { get; set; }

    public int ApartmentId { get; set; }

    [MaxLength(50)]
    public string? BillNumber { get; set; }

    public int VendorId { get; set; }
    public int ExpenseHeadId { get; set; }

    [Column(TypeName = "date")]
    public DateTime BillDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? DueDate { get; set; }

    [Precision(14, 2)]
    public decimal GrossAmount { get; set; }

    [Precision(14, 2)]
    public decimal TaxAmount { get; set; }

    [Precision(14, 2)]
    public decimal TdsAmount { get; set; }

    [Precision(14, 2)]
    public decimal NetPayable { get; set; }

    [Precision(14, 2)]
    public decimal AmountPaid { get; set; }

    [Precision(15, 2)]
    public decimal? OutstandingAmount { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(3000)]
    public string? Description { get; set; }
    public int? BudgetId { get; set; }
    public int? LockedInPeriodId { get; set; }

    [MaxLength(200)]
    public string? AttachmentFileName { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
