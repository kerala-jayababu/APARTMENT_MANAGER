using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("BankTransactions", Schema = "dbo")]
public sealed class BankTransaction
{
    [Key, Column("IdBankTransaction")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdBankTransaction { get; set; }

    public int ApartmentId { get; set; }
    public int BankAccountId { get; set; }

    [Column(TypeName = "date")]
    public DateTime TransactionDate { get; set; }

    [MaxLength(300)]
    public string Narration { get; set; } = string.Empty;

    [Precision(14, 2)]
    public decimal DebitAmount { get; set; }

    [Precision(14, 2)]
    public decimal CreditAmount { get; set; }

    [Precision(14, 2)]
    public decimal ClosingBalance { get; set; }

    [MaxLength(30)]
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
}
