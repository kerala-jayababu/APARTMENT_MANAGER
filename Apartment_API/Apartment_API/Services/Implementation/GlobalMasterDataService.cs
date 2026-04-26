using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class GlobalMasterDataService(AppDbContext db) : IGlobalMasterDataService
{
    public async Task<IReadOnlyList<AmenityTypeDto>> GetAmenityTypesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<AmenityType>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.AmenityTypeName)
            .Select(x => new AmenityTypeDto
            {
                IdAmenityType = x.IdAmenityType,
                AmenityTypeCode = x.AmenityTypeCode,
                AmenityTypeName = x.AmenityTypeName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BankAccountTypeDto>> GetBankAccountTypesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<BankAccountType>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.AccountTypeName)
            .Select(x => new BankAccountTypeDto
            {
                IdBankAccountType = x.IdBankAccountType,
                AccountTypeCode = x.AccountTypeCode,
                AccountTypeName = x.AccountTypeName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BookingChargeTypeDto>> GetBookingChargeTypesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<BookingChargeType>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ChargeTypeName)
            .Select(x => new BookingChargeTypeDto
            {
                IdBookingChargeType = x.IdBookingChargeType,
                ChargeTypeCode = x.ChargeTypeCode,
                ChargeTypeName = x.ChargeTypeName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CommitteeMemberStatusDto>> GetCommitteeMemberStatusesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<CommitteeMemberStatus>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.StatusName)
            .Select(x => new CommitteeMemberStatusDto
            {
                IdCommitteeMemberStatus = x.IdCommitteeMemberStatus,
                StatusCode = x.StatusCode,
                StatusName = x.StatusName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CommitteeRoleDto>> GetCommitteeRolesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<CommitteeRole>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.RoleName)
            .Select(x => new CommitteeRoleDto
            {
                IdCommitteeRole = x.IdCommitteeRole,
                RoleCode = x.RoleCode,
                RoleName = x.RoleName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ComplaintCategoryDto>> GetComplaintCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<ComplaintCategory>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CategoryName)
            .Select(x => new ComplaintCategoryDto
            {
                IdComplaintCategory = x.IdComplaintCategory,
                CategoryCode = x.CategoryCode,
                CategoryName = x.CategoryName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MasterComplaintStatusDto>> GetComplaintStatusesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<ComplaintStatus>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.StatusName)
            .Select(x => new MasterComplaintStatusDto
            {
                IdComplaintStatus = x.IdComplaintStatus,
                StatusCode = x.StatusCode,
                StatusName = x.StatusName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MasterDocumentCategoryDto>> GetDocumentCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<DocumentCategory>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CategoryName)
            .Select(x => new MasterDocumentCategoryDto
            {
                IdDocumentCategory = x.IdDocumentCategory,
                CategoryCode = x.CategoryCode,
                CategoryName = x.CategoryName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MasterExpenseStatusDto>> GetExpenseStatusesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<ExpenseStatus>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.StatusName)
            .Select(x => new MasterExpenseStatusDto
            {
                IdExpenseStatus = x.IdExpenseStatus,
                StatusCode = x.StatusCode,
                StatusName = x.StatusName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<IdentityDocTypeDto>> GetIdentityDocTypesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<IdentityDocType>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DocTypeName)
            .Select(x => new IdentityDocTypeDto
            {
                IdIdentityDocType = x.IdIdentityDocType,
                DocTypeCode = x.DocTypeCode,
                DocTypeName = x.DocTypeName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MasterInvoiceStatusDto>> GetInvoiceStatusesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<InvoiceStatus>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.StatusName)
            .Select(x => new MasterInvoiceStatusDto
            {
                IdInvoiceStatus = x.IdInvoiceStatus,
                StatusCode = x.StatusCode,
                StatusName = x.StatusName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MasterNoticeCategoryDto>> GetNoticeCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<NoticeCategory>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CategoryName)
            .Select(x => new MasterNoticeCategoryDto
            {
                IdNoticeCategory = x.IdNoticeCategory,
                CategoryCode = x.CategoryCode,
                CategoryName = x.CategoryName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OwnershipTypeDto>> GetOwnershipTypesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<OwnershipType>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.OwnershipName)
            .Select(x => new OwnershipTypeDto
            {
                IdOwnershipType = x.IdOwnershipType,
                OwnershipCode = x.OwnershipCode,
                OwnershipName = x.OwnershipName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentModeDto>> GetPaymentModesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<PaymentMode>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PaymentModeName)
            .Select(x => new PaymentModeDto
            {
                IdPaymentMode = x.IdPaymentMode,
                PaymentModeCode = x.PaymentModeCode,
                PaymentModeName = x.PaymentModeName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PersonTypeDto>> GetPersonTypesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<PersonType>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PersonTypeName)
            .Select(x => new PersonTypeDto
            {
                IdPersonType = x.IdPersonType,
                PersonTypeCode = x.PersonTypeCode,
                PersonTypeName = x.PersonTypeName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PriorityLevelDto>> GetPriorityLevelsAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<PriorityLevel>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PriorityName)
            .Select(x => new PriorityLevelDto
            {
                IdPriorityLevel = x.IdPriorityLevel,
                PriorityCode = x.PriorityCode,
                PriorityName = x.PriorityName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MasterReconciliationStatusDto>> GetReconciliationStatusesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<ReconciliationStatus>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.StatusName)
            .Select(x => new MasterReconciliationStatusDto
            {
                IdReconciliationStatus = x.IdReconciliationStatus,
                StatusCode = x.StatusCode,
                StatusName = x.StatusName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AppRoleListDto>> GetRolesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<AppRole>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.RoleName)
            .Select(x => new AppRoleListDto
            {
                IdRole = x.IdRole,
                RoleCode = x.RoleCode,
                RoleName = x.RoleName,
                Description = x.Description,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MasterUnitStatusDto>> GetUnitStatusesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<UnitStatus>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.StatusName)
            .Select(x => new MasterUnitStatusDto
            {
                IdUnitStatus = x.IdUnitStatus,
                StatusCode = x.StatusCode,
                StatusName = x.StatusName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UnitTypeDto>> GetUnitTypesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<UnitType>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.UnitTypeName)
            .Select(x => new UnitTypeDto
            {
                IdUnitType = x.IdUnitType,
                UnitTypeCode = x.UnitTypeCode,
                UnitTypeName = x.UnitTypeName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<GlobalMasterUserListDto>> GetUsersAsync(
        CancellationToken cancellationToken = default) =>
        await db.Users.AsNoTracking()
            .OrderBy(x => x.FullName)
            .Select(x => new GlobalMasterUserListDto
            {
                IdUser = x.IdUser,
                FullName = x.FullName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                IsSuperAdmin = x.IsSuperAdmin,
                IsActive = x.IsActive,
                LastLoginAt = x.LastLoginAt,
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy,
                Designation = x.Designation,
                ProfilePhotoUrl = x.ProfilePhotoUrl
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UtilityTypeDto>> GetUtilityTypesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<UtilityType>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.UtilityTypeName)
            .Select(x => new UtilityTypeDto
            {
                IdUtilityType = x.IdUtilityType,
                UtilityTypeCode = x.UtilityTypeCode,
                UtilityTypeName = x.UtilityTypeName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<VendorTypeDto>> GetVendorTypesAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<VendorType>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.VendorTypeName)
            .Select(x => new VendorTypeDto
            {
                IdVendorType = x.IdVendorType,
                VendorTypeCode = x.VendorTypeCode,
                VendorTypeName = x.VendorTypeName,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
}
