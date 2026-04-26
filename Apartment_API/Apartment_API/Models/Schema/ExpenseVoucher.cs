using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("ExpenseVouchers", Schema = "dbo")]
public sealed class ExpenseVoucher
{
    [Key, Column("IdExpenseVoucher")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdExpenseVoucher { get; set; }

    public int ApartmentId { get; set; }

    [MaxLength(30)]
    public string? VoucherNumber { get; set; }

    public int VendorId { get; set; }
    public int ExpenseHeadId { get; set; }
    public int FiscalYearId { get; set; }

    [MaxLength(100)]
    public string? VendorInvoice { get; set; }

    [Column(TypeName = "date")]
    public DateTime InvoiceDate { get; set; }

    [Precision(14, 2)]
    public decimal Amount { get; set; }

    [Precision(14, 2)]
    public decimal GstAmount { get; set; }

    [Precision(14, 2)]
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? SupportingDocUrl { get; set; }

    public int StatusId { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? VendorInvoiceNumber { get; set; }
}
