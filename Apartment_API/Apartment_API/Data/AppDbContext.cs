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
}
