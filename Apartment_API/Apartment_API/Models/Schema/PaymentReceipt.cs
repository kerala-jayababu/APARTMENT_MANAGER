using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("PaymentReceipts", Schema = "dbo")]
public sealed class PaymentReceipt
{
    [Key, Column("IdPaymentReceipt")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long IdPaymentReceipt { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(30)]
    public string ReceiptNumber { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime ReceiptDate { get; set; }

    public int? UnitId { get; set; }
    public int PersonId { get; set; }

    [MaxLength(20)]
    public string PaymentMode { get; set; } = string.Empty;

    public int? BankAccountId { get; set; }

    [MaxLength(20)]
    public string? ChequeNo { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ChequeDate { get; set; }

    [MaxLength(50)]
    public string? ReferenceNo { get; set; }

    [MaxLength(30)]
    public string? GatewayName { get; set; }

    [Precision(14, 2)]
    public decimal TotalAmount { get; set; }

    [Precision(14, 2)]
    public decimal AllocatedAmount { get; set; }

    [Precision(15, 2)]
    public decimal? UnallocatedAmount { get; set; }

    public bool IsAdvance { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Narration { get; set; }
    public int? LockedInPeriodId { get; set; }

    [MaxLength(3)]
    [MinLength(3)]
    public string CurrencyCode { get; set; } = "INR";

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
