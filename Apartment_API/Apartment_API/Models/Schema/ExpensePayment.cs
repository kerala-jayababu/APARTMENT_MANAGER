using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("ExpensePayments", Schema = "dbo")]
public sealed class ExpensePayment
{
    [Key, Column("IdExpensePayment")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long IdExpensePayment { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(30)]
    public string VoucherNo { get; set; } = string.Empty;

    public bool? IsAdvance { get; set; }

    [Column(TypeName = "date")]
    public DateTime PaymentDate { get; set; }

    public int VendorId { get; set; }

    [MaxLength(20)]
    public string PaymentMode { get; set; } = string.Empty;

    public int? BankAccountId { get; set; }

    [MaxLength(20)]
    public string? ChequeNo { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ChequeDate { get; set; }

    [MaxLength(50)]
    public string? ReferenceNo { get; set; }

    [Precision(14, 2)]
    public decimal PaidAmount { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Narration { get; set; }
    public int? LockedInPeriodId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
