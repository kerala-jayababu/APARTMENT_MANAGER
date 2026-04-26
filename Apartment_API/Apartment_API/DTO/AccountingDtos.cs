using System.ComponentModel.DataAnnotations;

namespace Apartment_API.DTO;

/// <summary>Result of a save: insert vs update, used to choose HTTP 201 or 200.</summary>
public sealed class EntitySaveResult<T>
{
    public T Data { get; init; } = default!;
    public bool Created { get; init; }
}

// ---- Bank account ----

public sealed class BankAccountDto
{
    public int IdBankAccount { get; init; }
    public int ApartmentId { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public string BankName { get; init; } = string.Empty;
    public int BankAccountTypeId { get; init; }
    public string AccountNumber { get; init; } = string.Empty;
    public string IfscCode { get; init; } = string.Empty;
    public string? BranchName { get; init; }
    public string AccountHolderName { get; init; } = string.Empty;
    public decimal OpeningBalance { get; init; }
    public decimal CurrentBalance { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public int CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int? UpdatedBy { get; init; }
    public int? LedgerAccountId { get; init; }
    public int? FundId { get; init; }
}

/// <summary><c>IdBankAccount = 0</c> to insert; existing id to update (must match <see cref="ApartmentId" />).</summary>
public sealed class BankAccountSaveDto
{
    public int IdBankAccount { get; set; }

    [Required]
    public int ApartmentId { get; set; }

    [Required, MaxLength(150)]
    public string AccountName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string BankName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int BankAccountTypeId { get; set; }

    [Required, MaxLength(50)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required, MaxLength(15)]
    public string IfscCode { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? BranchName { get; set; }

    [Required, MaxLength(200)]
    public string AccountHolderName { get; set; } = string.Empty;

    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public int? LedgerAccountId { get; set; }
    public int? FundId { get; set; }
}

// ---- Expense head ----

public sealed class ExpenseHeadDto
{
    public int IdExpenseHead { get; init; }
    public int? ApartmentId { get; init; }
    public string HeadCode { get; init; } = string.Empty;
    public string HeadName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public int CreatedBy { get; init; }
    public int? LedgerAccountId { get; init; }
}

/// <summary><c>IdExpenseHead = 0</c> to insert; existing id in body to update. <c>ApartmentId</c> in body for create/update as needed.</summary>
public sealed class ExpenseHeadSaveDto
{
    public int IdExpenseHead { get; set; }
    public int? ApartmentId { get; set; }

    [Required, MaxLength(50)]
    public string HeadCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string HeadName { get; set; } = string.Empty;

    [Range(0, 255)]
    public byte SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int? LedgerAccountId { get; set; }
}

// ---- Income head ----

public sealed class IncomeHeadDto
{
    public int IdIncomeHead { get; init; }
    public int? ApartmentId { get; init; }
    public string HeadCode { get; init; } = string.Empty;
    public string HeadName { get; init; } = string.Empty;
    public bool IsAutoInvoiced { get; init; }
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public int CreatedBy { get; init; }
    public int? LedgerAccountId { get; init; }
}

/// <summary><c>IdIncomeHead = 0</c> to insert; existing id in body to update. <c>ApartmentId</c> in body for create/update as needed.</summary>
public sealed class IncomeHeadSaveDto
{
    public int IdIncomeHead { get; set; }
    public int? ApartmentId { get; set; }

    [Required, MaxLength(50)]
    public string HeadCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string HeadName { get; set; } = string.Empty;
    public bool IsAutoInvoiced { get; set; }

    [Range(0, 255)]
    public byte SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int? LedgerAccountId { get; set; }
}

// ---- Vendor ----

public sealed class VendorDto
{
    public int IdVendor { get; init; }
    public int ApartmentId { get; init; }
    public string VendorCode { get; init; } = string.Empty;
    public string VendorName { get; init; } = string.Empty;
    public int VendorTypeId { get; init; }
    public string? ContactPerson { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? GstNumber { get; init; }
    public string? PanNumber { get; init; }
    public string? BankName { get; init; }
    public string? BankAccountNumber { get; init; }
    public string? IfscCode { get; init; }
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public int CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int? UpdatedBy { get; init; }
    public int? ControlLedgerAccountId { get; init; }
    public decimal OpeningPayable { get; init; }
}

/// <summary><c>IdVendor = 0</c> to insert; existing id to update (must match <see cref="ApartmentId" />).</summary>
public sealed class VendorSaveDto
{
    public int IdVendor { get; set; }

    [Required]
    public int ApartmentId { get; set; }

    [Required, MaxLength(20)]
    public string VendorCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string VendorName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int VendorTypeId { get; set; }

    [MaxLength(150)]
    public string? ContactPerson { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

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
    public int? ControlLedgerAccountId { get; set; }
    public decimal OpeningPayable { get; set; }
}
