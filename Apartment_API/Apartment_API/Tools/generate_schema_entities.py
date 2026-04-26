"""Generate Models/Schema/*.cs from Database_Schema_Documentation (one class per file). Run once; edit entities by hand if DB differs."""
from pathlib import Path

OUT = Path(__file__).resolve().parent.parent / "Models" / "Schema"
OUT.mkdir(parents=True, exist_ok=True)

HEADER = """using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;
"""


def w(name: str, body: str) -> None:
    (OUT / f"{name}.cs").write_text(HEADER + "\n" + body.strip() + "\n", encoding="utf-8")


# --- Chart of accounts & ledgers
w("AccountGroup", r'''
[Table("AccountGroups", Schema = "dbo")]
public sealed class AccountGroup
{
    [Key, Column("IDAccountGroup")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdAccountGroup { get; set; }

    public int? ApartmentId { get; set; }

    [Required, MaxLength(40)]
    public string GroupCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string GroupName { get; set; } = string.Empty;

    public int? ParentGroupId { get; set; }

    [Required, MaxLength(1)]
    [MinLength(1)]
    public string AccountType { get; set; } = string.Empty;

    [Required, MaxLength(1)]
    [MinLength(1)]
    public string NormalBalance { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? ReportSection { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public int? ModifiedByUserId { get; set; }
}
''')

w("LedgerAccount", r'''
[Table("LedgerAccounts", Schema = "dbo")]
public sealed class LedgerAccount
{
    [Key, Column("IDLedgerAccount")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdLedgerAccount { get; set; }

    public int ApartmentId { get; set; }
    public int GroupId { get; set; }
    public int? ParentLedgerId { get; set; }

    [Required, MaxLength(30)]
    public string AccountCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string AccountName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ShortName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsControl { get; set; }
    public int? SubLedgerTypeId { get; set; }
    public bool IsPosting { get; set; }
    public bool IsBankAccount { get; set; }
    public bool IsCashAccount { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }

    [Precision(18, 2)]
    public decimal OpeningBalance { get; set; }

    [MaxLength(1)]
    [MinLength(1)]
    public string OpeningBalanceSide { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime? OpeningBalanceAsOf { get; set; }

    public DateTime CreatedOn { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public int? ModifiedByUserId { get; set; }
}
''')

w("SubLedgerType", r'''
[Table("SubLedgerTypes", Schema = "dbo")]
public sealed class SubLedgerType
{
    [Key, Column("IDSubLedgerType")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdSubLedgerType { get; set; }

    [Required, MaxLength(30)]
    public string TypeCode { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string TypeName { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string ReferencedTable { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string ReferencedKeyColumn { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public int? CreatedByUserId { get; set; }
}
''')

# --- Apartment master (remaining)
w("Amenity", r'''
[Table("Amenities", Schema = "dbo")]
public sealed class Amenity
{
    [Key, Column("IdAmenity")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdAmenity { get; set; }

    public int ApartmentId { get; set; }
    public int AmenityTypeId { get; set; }

    [Required, MaxLength(150)]
    public string AmenityName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public short? Capacity { get; set; }
    public int ChargeTypeId { get; set; }

    [Precision(10, 2)]
    public decimal ChargeAmount { get; set; }

    public byte AdvanceBookingDays { get; set; }

    [MaxLength(2000)]
    public string? Rules { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
''')

w("Unit", r'''
[Table("Units", Schema = "dbo")]
public sealed class Unit
{
    [Key, Column("IdUnit")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdUnit { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(20)]
    public string UnitNumber { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Block { get; set; }

    public short Floor { get; set; }
    public int UnitTypeId { get; set; }

    [Precision(10, 2)]
    public decimal? BuiltUpArea { get; set; }

    [Precision(10, 2)]
    public decimal? CarpetArea { get; set; }

    [MaxLength(20)]
    public string? Facing { get; set; }

    public int UnitStatusId { get; set; }
    public int OwnershipTypeId { get; set; }
    public int? IdCurrentOwner { get; set; }

    [Precision(12, 2)]
    public decimal CurrentMmcAmount { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
''')

w("Budget", r'''
[Table("Budgets", Schema = "dbo")]
public sealed class Budget
{
    [Key, Column("IdBudget")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdBudget { get; set; }

    public int ApartmentId { get; set; }
    public int FiscalYearId { get; set; }
    public int ExpenseHeadId { get; set; }

    [Precision(14, 2)]
    public decimal BudgetAmount { get; set; }

    [Precision(14, 2)]
    public decimal ActualAmount { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
''')

w("FiscalYear", r'''
[Table("FiscalYears", Schema = "dbo")]
public sealed class FiscalYear
{
    [Key, Column("IdFiscalYear")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdFiscalYear { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(20)]
    public string FyCode { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime EndDate { get; set; }

    public bool IsCurrent { get; set; }
    public bool IsLocked { get; set; }
    public bool? IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
}
''')

w("Person", r'''
[Table("Persons", Schema = "dbo")]
public sealed class Person
{
    [Key, Column("IdPerson")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdPerson { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(20)]
    public string PersonNumber { get; set; } = string.Empty;

    public int PersonTypeId { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? AlternatePhone { get; set; }
    public int? IdentityDocTypeId { get; set; }

    [MaxLength(100)]
    public string? IdentityDocNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? PanNumber { get; set; }
    public int? LinkedUserId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? EmergencyContactName { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? EmergencyContactPhone { get; set; }
    public int? ParentOwnerId { get; set; }
}
''')

w("UnitOwner", r'''
[Table("UnitOwners", Schema = "dbo")]
public sealed class UnitOwner
{
    [Key, Column("IdUnitOwner")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdUnitOwner { get; set; }

    public int ApartmentId { get; set; }
    public int UnitId { get; set; }
    public int PersonId { get; set; }
    public bool IsPrimaryOwner { get; set; }

    [Column(TypeName = "date")]
    public DateTime OwnershipFromDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? OwnershipToDate { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [Precision(18, 2)]
    public decimal? TransferValue { get; set; }
}
''')

w("TenantAssignment", r'''
[Table("TenantAssignments", Schema = "dbo")]
public sealed class TenantAssignment
{
    [Key, Column("IdTenantAssignment")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdTenantAssignment { get; set; }

    public int ApartmentId { get; set; }
    public int UnitId { get; set; }
    public int PersonId { get; set; }

    [Column(TypeName = "date")]
    public DateTime LeaseStartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? LeaseEndDate { get; set; }

    [Precision(12, 2)]
    public decimal? MonthlyRent { get; set; }

    [Precision(12, 2)]
    public decimal? SecurityDeposit { get; set; }

    [MaxLength(500)]
    public string? AgreementDocUrl { get; set; }

    [Column(TypeName = "date")]
    public DateTime? VacatedDate { get; set; }

    [MaxLength(500)]
    public string? VacateRemarks { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
''')

w("UnitMmcDetail", r'''
[Table("UnitMMCDetails", Schema = "dbo")]
public sealed class UnitMmcDetail
{
    [Key, Column("IdUnitMMCDetail")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdUnitMmcDetail { get; set; }

    public int ApartmentId { get; set; }
    public int UnitId { get; set; }
    public int IncomeHeadId { get; set; }

    [Precision(10, 2)]
    public decimal MmcAmount { get; set; }

    [Column(TypeName = "date")]
    public DateTime MmcPeriodFrom { get; set; }

    [Column(TypeName = "date")]
    public DateTime? MmcPeriodTo { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
''')

w("CommitteeTenure", r'''
[Table("CommitteeTenures", Schema = "dbo")]
public sealed class CommitteeTenure
{
    [Key, Column("IdCommitteeTenure")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdCommitteeTenure { get; set; }

    public int ApartmentId { get; set; }

    [Column(TypeName = "date")]
    public DateTime TenureStartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime TenureEndDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
''')

w("CommitteeMember", r'''
[Table("CommitteeMembers", Schema = "dbo")]
public sealed class CommitteeMember
{
    [Key, Column("IdCommitteeMember")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdCommitteeMember { get; set; }

    public int ApartmentId { get; set; }
    public int CommitteeTenureId { get; set; }
    public int PersonId { get; set; }
    public int CommitteeRoleId { get; set; }

    [Column(TypeName = "date")]
    public DateTime EffectiveFromDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? EffectiveToDate { get; set; }

    public int StatusId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
''')

w("NotificationSetting", r'''
[Table("NotificationSettings", Schema = "dbo")]
public sealed class NotificationSetting
{
    [Key, Column("IdNotificationSetting")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdNotificationSetting { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(50)]
    public string EventCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    public bool IsSmsEnabled { get; set; }
    public bool IsEmailEnabled { get; set; }
    public bool IsAppPushEnabled { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
''')

w("RolePermission", r'''
[Table("RolePermissions", Schema = "dbo")]
public sealed class RolePermission
{
    [Key, Column("IdRolePermission")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdRolePermission { get; set; }

    public int ApartmentId { get; set; }
    public int RoleId { get; set; }

    [Required, MaxLength(50)]
    public string ModuleCode { get; set; } = string.Empty;

    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
''')

# --- Transactions
w("ExpenseBill", r'''
[Table("ExpenseBills", Schema = "dbo")]
public sealed class ExpenseBill
{
    [Key, Column("IdBill")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long IdBill { get; set; }

    public int ApartmentId { get; set; }

    [MaxLength(50)]
    public string? BillNumber { get; set; }

    public int VendorId { get; set; }
    public int ExpenseHeadId { get; set; }

    [Column(TypeName = "date")]
    public DateTime BillDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? DueDate { get; set; }

    [Precision(14, 2)]
    public decimal GrossAmount { get; set; }

    [Precision(14, 2)]
    public decimal TaxAmount { get; set; }

    [Precision(14, 2)]
    public decimal TdsAmount { get; set; }

    [Precision(14, 2)]
    public decimal NetPayable { get; set; }

    [Precision(14, 2)]
    public decimal AmountPaid { get; set; }

    [Precision(15, 2)]
    public decimal? OutstandingAmount { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(3000)]
    public string? Description { get; set; }
    public int? BudgetId { get; set; }
    public int? LockedInPeriodId { get; set; }

    [MaxLength(200)]
    public string? AttachmentFileName { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
''')

w("ExpensePayment", r'''
[Table("ExpensePayments", Schema = "dbo")]
public sealed class ExpensePayment
{
    [Key, Column("IdExpensePayment")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long IdExpensePayment { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(30)]
    public string VoucherNo { get; set; } = string.Empty;

    public bool? IsAdvance { get; set; }

    [Column(TypeName = "date")]
    public DateTime PaymentDate { get; set; }

    public int VendorId { get; set; }

    [MaxLength(20)]
    public string PaymentMode { get; set; } = string.Empty;

    public int? BankAccountId { get; set; }

    [MaxLength(20)]
    public string? ChequeNo { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ChequeDate { get; set; }

    [MaxLength(50)]
    public string? ReferenceNo { get; set; }

    [Precision(14, 2)]
    public decimal PaidAmount { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Narration { get; set; }
    public int? LockedInPeriodId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
''')

w("ExpenseBillPayment", r'''
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
''')

w("Invoice", r'''
[Table("Invoices", Schema = "dbo")]
public sealed class Invoice
{
    [Key, Column("IdInvoice")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdInvoice { get; set; }

    public int ApartmentId { get; set; }

    [MaxLength(30)]
    public string? InvoiceSource { get; set; }

    public int UnitId { get; set; }
    public int PersonId { get; set; }
    public int IncomeHeadId { get; set; }
    public int FiscalYearId { get; set; }

    [Column(TypeName = "date")]
    public DateTime InvoicePeriodStart { get; set; }

    [Column(TypeName = "date")]
    public DateTime InvoicePeriodEnd { get; set; }

    [Column(TypeName = "date")]
    public DateTime InvoiceDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime DueDate { get; set; }

    [Precision(14, 2)]
    public decimal Amount { get; set; }

    [Precision(14, 2)]
    public decimal GstAmount { get; set; }

    [Precision(14, 2)]
    public decimal TotalAmount { get; set; }

    [Precision(14, 2)]
    public decimal PaidAmount { get; set; }

    [Precision(15, 2)]
    public decimal? BalanceAmount { get; set; }

    public int StatusId { get; set; }
    public bool IsAutoGenerated { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? InvoiceNumber { get; set; }

    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
''')

w("PaymentReceipt", r'''
[Table("PaymentReceipts", Schema = "dbo")]
public sealed class PaymentReceipt
{
    [Key, Column("IdPaymentReceipt")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long IdPaymentReceipt { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(30)]
    public string ReceiptNumber { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime ReceiptDate { get; set; }

    public int? UnitId { get; set; }
    public int PersonId { get; set; }

    [MaxLength(20)]
    public string PaymentMode { get; set; } = string.Empty;

    public int? BankAccountId { get; set; }

    [MaxLength(20)]
    public string? ChequeNo { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ChequeDate { get; set; }

    [MaxLength(50)]
    public string? ReferenceNo { get; set; }

    [MaxLength(30)]
    public string? GatewayName { get; set; }

    [Precision(14, 2)]
    public decimal TotalAmount { get; set; }

    [Precision(14, 2)]
    public decimal AllocatedAmount { get; set; }

    [Precision(15, 2)]
    public decimal? UnallocatedAmount { get; set; }

    public bool IsAdvance { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Narration { get; set; }
    public int? LockedInPeriodId { get; set; }

    [MaxLength(3)]
    [MinLength(3)]
    public string CurrencyCode { get; set; } = "INR";

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
''')

w("InvoiceReceipt", r'''
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
''')

w("AmenityBooking", r'''
[Table("AmenityBookings", Schema = "dbo")]
public sealed class AmenityBooking
{
    [Key, Column("IdAmenityBooking")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdAmenityBooking { get; set; }

    public int ApartmentId { get; set; }
    public int AmenityId { get; set; }
    public int UnitId { get; set; }
    public int PersonId { get; set; }

    [Column(TypeName = "date")]
    public DateTime BookingDate { get; set; }

    [Column(TypeName = "time")]
    public TimeSpan SlotStartTime { get; set; }

    [Column(TypeName = "time")]
    public TimeSpan SlotEndTime { get; set; }

    [Precision(10, 2)]
    public decimal ChargeAmount { get; set; }
    public int? InvoiceId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public int? StatusId { get; set; }
}
''')

w("BankTransaction", r'''
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
''')

w("PettyCashTransaction", r'''
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
''')

w("UtilityBillBatch", r'''
[Table("UtilityBillBatches", Schema = "dbo")]
public sealed class UtilityBillBatch
{
    [Key, Column("IdUtilityBillBatch")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdUtilityBillBatch { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(30)]
    public string BatchCode { get; set; } = string.Empty;

    public int UtilityTypeId { get; set; }

    [Column(TypeName = "date")]
    public DateTime BillingPeriodStart { get; set; }

    [Column(TypeName = "date")]
    public DateTime BillingPeriodEnd { get; set; }

    [Column(TypeName = "date")]
    public DateTime BillDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime DueDate { get; set; }

    [Precision(14, 2)]
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsPosted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ApplyToBlock { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ApplyToScope { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? BillingMethod { get; set; }
}
''')

w("UtilityBillBatchLine", r'''
[Table("UtilityBillBatchLines", Schema = "dbo")]
public sealed class UtilityBillBatchLine
{
    [Key, Column("IdUtilityBillBatchLine")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdUtilityBillBatchLine { get; set; }

    public int ApartmentId { get; set; }
    public int UtilityBillBatchId { get; set; }
    public int UnitId { get; set; }

    [Precision(12, 2)]
    public decimal Amount { get; set; }
    public int? InvoiceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
}
''')

w("Reconciliation", r'''
[Table("Reconciliations", Schema = "dbo")]
public sealed class Reconciliation
{
    [Key, Column("IdReconciliation")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdReconciliation { get; set; }

    public int ApartmentId { get; set; }
    public int FiscalYearId { get; set; }

    [MaxLength(10)]
    public string PeriodType { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime PeriodStartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime PeriodEndDate { get; set; }

    [Precision(14, 2)]
    public decimal TotalIncome { get; set; }

    [Precision(14, 2)]
    public decimal TotalExpense { get; set; }

    [Precision(15, 2)]
    public decimal? NetSurplus { get; set; }

    public int StatusId { get; set; }
    public int InitiatedBy { get; set; }
    public DateTime InitiatedAt { get; set; }
    public int? TreasurerApprovedBy { get; set; }
    public DateTime? TreasurerApprovedAt { get; set; }
    public int? SecretaryApprovedBy { get; set; }
    public DateTime? SecretaryApprovedAt { get; set; }
    public DateTime? LockedAt { get; set; }

    [MaxLength(1000)]
    public string? ReturnRemarks { get; set; }

    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ReconciliationRef { get; set; }
}
''')

w("StoredDocument", r'''
[Table("Documents", Schema = "dbo")]
public sealed class StoredDocument
{
    [Key, Column("IdDocument")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdDocument { get; set; }

    public int ApartmentId { get; set; }
    public int CategoryId { get; set; }

    [Required, MaxLength(300)]
    public string DocumentName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? FileUrl { get; set; }
    public int? FileSizeKb { get; set; }

    [MaxLength(100)]
    public string? MimeType { get; set; }

    [MaxLength(30)]
    public string? LinkedEntityType { get; set; }
    public int? LinkedEntityId { get; set; }
    public int UploadedByUserId { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
}
''')

w("Notice", r'''
[Table("Notices", Schema = "dbo")]
public sealed class Notice
{
    [Key, Column("IdNotice")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdNotice { get; set; }

    public int ApartmentId { get; set; }
    public int CategoryId { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string Body { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime PublishDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    [MaxLength(20)]
    public string Visibility { get; set; } = string.Empty;

    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int? PublishedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
''')

w("Complaint", r'''
[Table("Complaints", Schema = "dbo")]
public sealed class Complaint
{
    [Key, Column("IdComplaint")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdComplaint { get; set; }

    public int ApartmentId { get; set; }

    [MaxLength(20)]
    public string? TicketNumber { get; set; }

    public int UnitId { get; set; }
    public int RaisedByPersonId { get; set; }
    public int CategoryId { get; set; }
    public int PriorityId { get; set; }

    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
    public int? AssignedToUserId { get; set; }
    public int StatusId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    [MaxLength(1000)]
    public string? ResolutionNotes { get; set; }

    [MaxLength(500)]
    public string? AttachmentUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
''')

w("ComplaintUpdate", r'''
[Table("ComplaintUpdates", Schema = "dbo")]
public sealed class ComplaintUpdate
{
    [Key, Column("IdComplaintUpdate")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdComplaintUpdate { get; set; }

    public int ApartmentId { get; set; }
    public int ComplaintId { get; set; }
    public int UpdatedByUserId { get; set; }
    public int? PreviousStatusId { get; set; }
    public int? NewStatusId { get; set; }

    [MaxLength(1000)]
    public string Remarks { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
''')

w("AuditLog", r'''
[Table("AuditLog", Schema = "dbo")]
public sealed class AuditLog
{
    [Key, Column("IdAuditLog")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long IdAuditLog { get; set; }

    public int? ApartmentId { get; set; }
    public int? UserId { get; set; }

    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TableName { get; set; }
    public int? RecordId { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? OldValues { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? NewValues { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }
    public DateTime OccurredAt { get; set; }
}
''')

w("ExpenseVoucher", r'''
[Table("ExpenseVouchers", Schema = "dbo")]
public sealed class ExpenseVoucher
{
    [Key, Column("IdExpenseVoucher")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdExpenseVoucher { get; set; }

    public int ApartmentId { get; set; }

    [MaxLength(30)]
    public string? VoucherNumber { get; set; }

    public int VendorId { get; set; }
    public int ExpenseHeadId { get; set; }
    public int FiscalYearId { get; set; }

    [MaxLength(100)]
    public string? VendorInvoice { get; set; }

    [Column(TypeName = "date")]
    public DateTime InvoiceDate { get; set; }

    [Precision(14, 2)]
    public decimal Amount { get; set; }

    [Precision(14, 2)]
    public decimal GstAmount { get; set; }

    [Precision(14, 2)]
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? SupportingDocUrl { get; set; }

    public int StatusId { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? VendorInvoiceNumber { get; set; }
}
''')

w("LegacyReceipt", r'''
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
''')

if __name__ == "__main__":
    print("Wrote", len(list(OUT.glob("*.cs"))), "files to", OUT)
