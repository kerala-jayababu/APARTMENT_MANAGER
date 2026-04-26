using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("BankAccounts", Schema = "dbo")]
public sealed class BankAccount
{
    [Key]
    [Column("IdBankAccount")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdBankAccount { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(150)]
    public string AccountName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string BankName { get; set; } = string.Empty;

    public int BankAccountTypeId { get; set; }

    [Required, MaxLength(50)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required, MaxLength(15)]
    public string IfscCode { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? BranchName { get; set; }

    [Required, MaxLength(200)]
    public string AccountHolderName { get; set; } = string.Empty;

    [Precision(14, 2)]
    public decimal OpeningBalance { get; set; }

    [Precision(14, 2)]
    public decimal CurrentBalance { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    [Column("LedgerAccountID")]
    public int? LedgerAccountId { get; set; }

    [Column("FundID")]
    public int? FundId { get; set; }
}
