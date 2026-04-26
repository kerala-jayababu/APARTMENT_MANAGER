using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("PettyCashTransactions", Schema = "dbo")]
public sealed class PettyCashTransaction
{
    [Key, Column("IdPettyCashTransaction")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdPettyCashTransaction { get; set; }

    public int ApartmentId { get; set; }

    [Column(TypeName = "date")]
    public DateTime TransactionDate { get; set; }

    [Required, MaxLength(20)]
    public string TransactionType { get; set; } = string.Empty;

    public int? BankAccountId { get; set; }
    public int? ExpenseHeadId { get; set; }

    [Precision(12, 2)]
    public decimal Amount { get; set; }

    [MaxLength(150)]
    public string? Payee { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    [Precision(12, 2)]
    public decimal ClosingCashBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ChequeNumber { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? VoucherNumber { get; set; }
}
