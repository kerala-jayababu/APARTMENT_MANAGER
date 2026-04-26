using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IGlobalMasterDataService
{
    Task<IReadOnlyList<AmenityTypeDto>> GetAmenityTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BankAccountTypeDto>> GetBankAccountTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookingChargeTypeDto>> GetBookingChargeTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommitteeMemberStatusDto>> GetCommitteeMemberStatusesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommitteeRoleDto>> GetCommitteeRolesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComplaintCategoryDto>> GetComplaintCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MasterComplaintStatusDto>> GetComplaintStatusesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MasterDocumentCategoryDto>> GetDocumentCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MasterExpenseStatusDto>> GetExpenseStatusesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdentityDocTypeDto>> GetIdentityDocTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MasterInvoiceStatusDto>> GetInvoiceStatusesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MasterNoticeCategoryDto>> GetNoticeCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OwnershipTypeDto>> GetOwnershipTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentModeDto>> GetPaymentModesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PersonTypeDto>> GetPersonTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriorityLevelDto>> GetPriorityLevelsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MasterReconciliationStatusDto>> GetReconciliationStatusesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppRoleListDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MasterUnitStatusDto>> GetUnitStatusesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnitTypeDto>> GetUnitTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlobalMasterUserListDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UtilityTypeDto>> GetUtilityTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VendorTypeDto>> GetVendorTypesAsync(CancellationToken cancellationToken = default);
}
