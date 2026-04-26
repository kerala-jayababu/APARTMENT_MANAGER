using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("InvoiceReceipts", Schema = "dbo")]
public sealed class InvoiceReceipt
{
    [Key, Column("IdInvoiceReceipt")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long IdInvoiceReceipt { get; set; }

    public int InvoiceId { get; set; }
    public long PaymentReceiptId { get; set; }

    [Precision(14, 2)]
    public decimal AllocatedAmount { get; set; }

    [Column(TypeName = "date")]
    public DateTime AllocationDate { get; set; }

    [MaxLength(200)]
    public string? Remarks { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
