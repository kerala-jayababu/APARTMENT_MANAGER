using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("Receipts", Schema = "dbo")]
public sealed class LegacyReceipt
{
    [Key, Column("IdReceipt")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdReceipt { get; set; }

    public int ApartmentId { get; set; }

    [MaxLength(30)]
    public string? ReceiptNumber { get; set; }

    public int InvoiceId { get; set; }
    public int UnitId { get; set; }
    public int PersonId { get; set; }
    public int PaymentModeId { get; set; }

    [Column(TypeName = "date")]
    public DateTime PaymentDate { get; set; }

    [Precision(14, 2)]
    public decimal AmountReceived { get; set; }

    [MaxLength(100)]
    public string? TransactionReference { get; set; }
    public int? BankAccountId { get; set; }
    public bool IsOfflineVerified { get; set; }
    public int? OfflineUploadedBy { get; set; }
    public int? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ChequeBankName { get; set; }
    public int? IncomeHeadId { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? PayerName { get; set; }
}
