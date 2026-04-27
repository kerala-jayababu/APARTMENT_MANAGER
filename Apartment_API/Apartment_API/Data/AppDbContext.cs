using Apartment_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<AmenityType> AmenityTypes => Set<AmenityType>();
    public DbSet<BankAccountType> BankAccountTypes => Set<BankAccountType>();
    public DbSet<BookingChargeType> BookingChargeTypes => Set<BookingChargeType>();
    public DbSet<CommitteeMemberStatus> CommitteeMemberStatuses => Set<CommitteeMemberStatus>();
    public DbSet<CommitteeRole> CommitteeRoles => Set<CommitteeRole>();
    public DbSet<ComplaintCategory> ComplaintCategories => Set<ComplaintCategory>();
    public DbSet<ComplaintStatus> ComplaintStatuses => Set<ComplaintStatus>();
    public DbSet<DocumentCategory> DocumentCategories => Set<DocumentCategory>();
    public DbSet<ExpenseStatus> ExpenseStatuses => Set<ExpenseStatus>();
    public DbSet<IdentityDocType> IdentityDocTypes => Set<IdentityDocType>();
    public DbSet<InvoiceStatus> InvoiceStatuses => Set<InvoiceStatus>();
    public DbSet<NoticeCategory> NoticeCategories => Set<NoticeCategory>();
    public DbSet<OwnershipType> OwnershipTypes => Set<OwnershipType>();
    public DbSet<PaymentMode> PaymentModes => Set<PaymentMode>();
    public DbSet<PersonType> PersonTypes => Set<PersonType>();
    public DbSet<PriorityLevel> PriorityLevels => Set<PriorityLevel>();
    public DbSet<ReconciliationStatus> ReconciliationStatuses => Set<ReconciliationStatus>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<UnitStatus> UnitStatuses => Set<UnitStatus>();
    public DbSet<UnitType> UnitTypes => Set<UnitType>();
    public DbSet<UtilityType> UtilityTypes => Set<UtilityType>();
    public DbSet<VendorType> VendorTypes => Set<VendorType>();

    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetHeader> BudgetHeaders => Set<BudgetHeader>();
    public DbSet<BudgetRevision> BudgetRevisions => Set<BudgetRevision>();
    public DbSet<ExpenseHead> ExpenseHeads => Set<ExpenseHead>();
    public DbSet<ExpenseMainHead> ExpenseMainHeads => Set<ExpenseMainHead>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<IncomeHead> IncomeHeads => Set<IncomeHead>();
    public DbSet<Vendor> Vendors => Set<Vendor>();

    public DbSet<Apartment> Apartments => Set<Apartment>();
    public DbSet<ApartmentUser> ApartmentUsers => Set<ApartmentUser>();
    public DbSet<ApprovalFlow> ApprovalFlows => Set<ApprovalFlow>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<AmenityBooking> AmenityBookings => Set<AmenityBooking>();
    public DbSet<CommitteeTenure> CommitteeTenures => Set<CommitteeTenure>();
    public DbSet<CommitteeMember> CommitteeMembers => Set<CommitteeMember>();
    public DbSet<CommitteeTenureExtensionLog> CommitteeTenureExtensionLogs => Set<CommitteeTenureExtensionLog>();
    public DbSet<MmcPeriod> MmcPeriods => Set<MmcPeriod>();
    public DbSet<MmcBatch> MmcBatches => Set<MmcBatch>();
    public DbSet<MmcBatchLine> MmcBatchLines => Set<MmcBatchLine>();

    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<UnitOwner> UnitOwners => Set<UnitOwner>();
    public DbSet<TenantAssignment> TenantAssignments => Set<TenantAssignment>();
    public DbSet<UnitMmcDetail> UnitMmcDetails => Set<UnitMmcDetail>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<UnitStatusHistory> UnitStatusHistory => Set<UnitStatusHistory>();
    public DbSet<OwnershipHistory> OwnershipHistory => Set<OwnershipHistory>();
    public DbSet<StoredDocument> StoredDocuments => Set<StoredDocument>();
}
