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

[Table("ExpenseHeads", Schema = "dbo")]
public sealed class ExpenseHead
{
    [Key]
    [Column("IdExpenseHead")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdExpenseHead { get; set; }

    public int? ApartmentId { get; set; }

    [Required, MaxLength(50)]
    public string HeadCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string HeadName { get; set; } = string.Empty;

    public byte SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }

    [Column("LedgerAccountID")]
    public int? LedgerAccountId { get; set; }
}

[Table("IncomeHeads", Schema = "dbo")]
public sealed class IncomeHead
{
    [Key]
    [Column("IdIncomeHead")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdIncomeHead { get; set; }

    public int? ApartmentId { get; set; }

    [Required, MaxLength(50)]
    public string HeadCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string HeadName { get; set; } = string.Empty;

    public bool IsAutoInvoiced { get; set; }
    public byte SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }

    [Column("LedgerAccountID")]
    public int? LedgerAccountId { get; set; }
}

[Table("Vendors", Schema = "dbo")]
public sealed class Vendor
{
    [Key]
    [Column("IdVendor")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdVendor { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(20)]
    public string VendorCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string VendorName { get; set; } = string.Empty;

    public int VendorTypeId { get; set; }

    [MaxLength(150)]
    public string? ContactPerson { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? PhoneNumber { get; set; }

    [MaxLength(20)]
    public string? GstNumber { get; set; }

    [MaxLength(20)]
    public string? PanNumber { get; set; }

    [MaxLength(100)]
    public string? BankName { get; set; }

    [MaxLength(50)]
    public string? BankAccountNumber { get; set; }

    [MaxLength(15)]
    public string? IfscCode { get; set; }

    [MaxLength(200)]
    public string? AddressLine1 { get; set; }

    [MaxLength(200)]
    public string? AddressLine2 { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [Column("ControlLedgerAccountID")]
    public int? ControlLedgerAccountId { get; set; }

    [Precision(18, 2)]
    public decimal OpeningPayable { get; set; }
}
