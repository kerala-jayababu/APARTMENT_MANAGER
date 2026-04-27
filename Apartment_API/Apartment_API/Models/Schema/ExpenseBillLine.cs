using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("ExpenseBillLines", Schema = "dbo")]
public sealed class ExpenseBillLine
{
    [Key, Column("IdExpenseBillLine")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long IdExpenseBillLine { get; set; }

    public long BillId { get; set; }
    public int ExpenseHeadId { get; set; }
    public bool IsAsset { get; set; }

    [MaxLength(100)]
    public string? Description { get; set; }

    [MaxLength(30)]
    public string? HsnSacCode { get; set; }

    [Precision(14, 2)]
    public decimal Quantity { get; set; }

    [Precision(14, 2)]
    public decimal UnitPrice { get; set; }

    [Precision(14, 2)]
    public decimal Discount { get; set; }

    [Precision(14, 2)]
    public decimal TaxableAmount { get; set; }

    [Precision(6, 2)]
    public decimal GstPercentage { get; set; }

    [Precision(14, 2)]
    public decimal GstAmount { get; set; }

    [Precision(14, 2)]
    public decimal LineTotal { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
