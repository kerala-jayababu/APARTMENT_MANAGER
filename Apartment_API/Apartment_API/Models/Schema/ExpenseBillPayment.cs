using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("ExpenseBillPayments", Schema = "dbo")]
public sealed class ExpenseBillPayment
{
    [Key, Column("IdExpenseBillPayment")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long IdExpenseBillPayment { get; set; }

    public long ExpensePaymentId { get; set; }
    public long BillId { get; set; }

    [Precision(14, 2)]
    public decimal AllocatedAmount { get; set; }

    [Column(TypeName = "date")]
    public DateTime AllocationDate { get; set; }

    [MaxLength(200)]
    public string? Remarks { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
