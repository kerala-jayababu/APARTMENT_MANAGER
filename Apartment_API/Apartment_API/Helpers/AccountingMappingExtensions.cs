using Apartment_API.DTO;
using Apartment_API.Models;

namespace Apartment_API.Helpers;

public static class AccountingMappingExtensions
{
    public static BankAccountDto ToDto(this BankAccount e) => new()
    {
        IdBankAccount = e.IdBankAccount,
        ApartmentId = e.ApartmentId,
        AccountName = e.AccountName,
        BankName = e.BankName,
        BankAccountTypeId = e.BankAccountTypeId,
        AccountNumber = e.AccountNumber,
        IfscCode = e.IfscCode,
        BranchName = e.BranchName,
        AccountHolderName = e.AccountHolderName,
        OpeningBalance = e.OpeningBalance,
        CurrentBalance = e.CurrentBalance,
        IsActive = e.IsActive,
        CreatedAt = e.CreatedAt,
        CreatedBy = e.CreatedBy,
        UpdatedAt = e.UpdatedAt,
        UpdatedBy = e.UpdatedBy,
        LedgerAccountId = e.LedgerAccountId,
        FundId = e.FundId
    };

    public static ExpenseHeadDto ToDto(this ExpenseHead e) => new()
    {
        IdExpenseHead = e.IdExpenseHead,
        ApartmentId = e.ApartmentId,
        HeadCode = e.HeadCode,
        HeadName = e.HeadName,
        SortOrder = e.SortOrder,
        IsActive = e.IsActive,
        CreatedAt = e.CreatedAt,
        CreatedBy = e.CreatedBy,
        LedgerAccountId = e.LedgerAccountId
    };

    public static IncomeHeadDto ToDto(this IncomeHead e) => new()
    {
        IdIncomeHead = e.IdIncomeHead,
        ApartmentId = e.ApartmentId,
        HeadCode = e.HeadCode,
        HeadName = e.HeadName,
        IsAutoInvoiced = e.IsAutoInvoiced,
        SortOrder = e.SortOrder,
        IsActive = e.IsActive,
        CreatedAt = e.CreatedAt,
        CreatedBy = e.CreatedBy,
        LedgerAccountId = e.LedgerAccountId
    };

    public static VendorDto ToDto(this Vendor e) => new()
    {
        IdVendor = e.IdVendor,
        ApartmentId = e.ApartmentId,
        VendorCode = e.VendorCode,
        VendorName = e.VendorName,
        VendorTypeId = e.VendorTypeId,
        ContactPerson = e.ContactPerson,
        Email = e.Email,
        PhoneNumber = e.PhoneNumber,
        GstNumber = e.GstNumber,
        PanNumber = e.PanNumber,
        BankName = e.BankName,
        BankAccountNumber = e.BankAccountNumber,
        IfscCode = e.IfscCode,
        AddressLine1 = e.AddressLine1,
        AddressLine2 = e.AddressLine2,
        Notes = e.Notes,
        IsActive = e.IsActive,
        CreatedAt = e.CreatedAt,
        CreatedBy = e.CreatedBy,
        UpdatedAt = e.UpdatedAt,
        UpdatedBy = e.UpdatedBy,
        ControlLedgerAccountId = e.ControlLedgerAccountId,
        OpeningPayable = e.OpeningPayable
    };
}
