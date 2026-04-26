using Asp.Versioning;
using Apartment_API.Configuration;
using Apartment_API.DTO;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apartment_API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ApiAccess)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class GlobalMasterDataController(
    IGlobalMasterDataService globalMasterDataService,
    ILogger<GlobalMasterDataController> logger) : ControllerBase
{
    [HttpGet("amenity-types")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<AmenityTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<AmenityTypeDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<AmenityTypeDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<AmenityTypeDto>>>> GetAmenityTypes(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetAmenityTypesAsync(cancellationToken),
            "Amenity types loaded.",
            "GetAmenityTypes");

    [HttpGet("bank-account-types")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<BankAccountTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<BankAccountTypeDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<BankAccountTypeDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<BankAccountTypeDto>>>> GetBankAccountTypes(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetBankAccountTypesAsync(cancellationToken),
            "Bank account types loaded.",
            "GetBankAccountTypes");

    [HttpGet("booking-charge-types")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<BookingChargeTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<BookingChargeTypeDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<BookingChargeTypeDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<BookingChargeTypeDto>>>> GetBookingChargeTypes(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetBookingChargeTypesAsync(cancellationToken),
            "Booking charge types loaded.",
            "GetBookingChargeTypes");

    [HttpGet("committee-member-statuses")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<CommitteeMemberStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<CommitteeMemberStatusDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<CommitteeMemberStatusDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<CommitteeMemberStatusDto>>>>
        GetCommitteeMemberStatuses(CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetCommitteeMemberStatusesAsync(cancellationToken),
            "Committee member statuses loaded.",
            "GetCommitteeMemberStatuses");

    [HttpGet("committee-roles")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<CommitteeRoleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<CommitteeRoleDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<CommitteeRoleDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<CommitteeRoleDto>>>> GetCommitteeRoles(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetCommitteeRolesAsync(cancellationToken),
            "Committee roles loaded.",
            "GetCommitteeRoles");

    [HttpGet("complaint-categories")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<ComplaintCategoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<ComplaintCategoryDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<ComplaintCategoryDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<ComplaintCategoryDto>>>> GetComplaintCategories(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetComplaintCategoriesAsync(cancellationToken),
            "Complaint categories loaded.",
            "GetComplaintCategories");

    [HttpGet("complaint-statuses")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterComplaintStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterComplaintStatusDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterComplaintStatusDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<MasterComplaintStatusDto>>>> GetComplaintStatuses(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetComplaintStatusesAsync(cancellationToken),
            "Complaint statuses loaded.",
            "GetComplaintStatuses");

    [HttpGet("document-categories")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterDocumentCategoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterDocumentCategoryDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterDocumentCategoryDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<MasterDocumentCategoryDto>>>>
        GetDocumentCategories(CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetDocumentCategoriesAsync(cancellationToken),
            "Document categories loaded.",
            "GetDocumentCategories");

    [HttpGet("expense-statuses")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterExpenseStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterExpenseStatusDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterExpenseStatusDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<MasterExpenseStatusDto>>>> GetExpenseStatuses(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetExpenseStatusesAsync(cancellationToken),
            "Expense statuses loaded.",
            "GetExpenseStatuses");

    [HttpGet("identity-doc-types")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<IdentityDocTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<IdentityDocTypeDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<IdentityDocTypeDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<IdentityDocTypeDto>>>> GetIdentityDocTypes(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetIdentityDocTypesAsync(cancellationToken),
            "Identity document types loaded.",
            "GetIdentityDocTypes");

    [HttpGet("invoice-statuses")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterInvoiceStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterInvoiceStatusDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterInvoiceStatusDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<MasterInvoiceStatusDto>>>> GetInvoiceStatuses(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetInvoiceStatusesAsync(cancellationToken),
            "Invoice statuses loaded.",
            "GetInvoiceStatuses");

    [HttpGet("notice-categories")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterNoticeCategoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterNoticeCategoryDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterNoticeCategoryDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<MasterNoticeCategoryDto>>>> GetNoticeCategories(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetNoticeCategoriesAsync(cancellationToken),
            "Notice categories loaded.",
            "GetNoticeCategories");

    [HttpGet("ownership-types")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<OwnershipTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<OwnershipTypeDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<OwnershipTypeDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<OwnershipTypeDto>>>> GetOwnershipTypes(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetOwnershipTypesAsync(cancellationToken),
            "Ownership types loaded.",
            "GetOwnershipTypes");

    [HttpGet("payment-modes")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<PaymentModeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<PaymentModeDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<PaymentModeDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<PaymentModeDto>>>> GetPaymentModes(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetPaymentModesAsync(cancellationToken),
            "Payment modes loaded.",
            "GetPaymentModes");

    [HttpGet("person-types")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<PersonTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<PersonTypeDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<PersonTypeDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<PersonTypeDto>>>> GetPersonTypes(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetPersonTypesAsync(cancellationToken),
            "Person types loaded.",
            "GetPersonTypes");

    [HttpGet("priority-levels")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<PriorityLevelDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<PriorityLevelDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<PriorityLevelDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<PriorityLevelDto>>>> GetPriorityLevels(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetPriorityLevelsAsync(cancellationToken),
            "Priority levels loaded.",
            "GetPriorityLevels");

    [HttpGet("reconciliation-statuses")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterReconciliationStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterReconciliationStatusDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterReconciliationStatusDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<MasterReconciliationStatusDto>>>>
        GetReconciliationStatuses(CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetReconciliationStatusesAsync(cancellationToken),
            "Reconciliation statuses loaded.",
            "GetReconciliationStatuses");

    [HttpGet("roles")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<AppRoleListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<AppRoleListDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<AppRoleListDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<AppRoleListDto>>>> GetRoles(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetRolesAsync(cancellationToken),
            "Roles loaded.",
            "GetRoles");

    [HttpGet("unit-statuses")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterUnitStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterUnitStatusDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterUnitStatusDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<MasterUnitStatusDto>>>> GetUnitStatuses(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetUnitStatusesAsync(cancellationToken),
            "Unit statuses loaded.",
            "GetUnitStatuses");

    [HttpGet("unit-types")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<UnitTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<UnitTypeDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<UnitTypeDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<UnitTypeDto>>>> GetUnitTypes(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetUnitTypesAsync(cancellationToken),
            "Unit types loaded.",
            "GetUnitTypes");

    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<GlobalMasterUserListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<GlobalMasterUserListDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<GlobalMasterUserListDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<GlobalMasterUserListDto>>>> GetUsers(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetUsersAsync(cancellationToken),
            "Users loaded.",
            "GetUsers");

    [HttpGet("utility-types")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<UtilityTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<UtilityTypeDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<UtilityTypeDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<UtilityTypeDto>>>> GetUtilityTypes(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetUtilityTypesAsync(cancellationToken),
            "Utility types loaded.",
            "GetUtilityTypes");

    [HttpGet("vendor-types")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<VendorTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<VendorTypeDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<VendorTypeDto>>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<VendorTypeDto>>>> GetVendorTypes(
        CancellationToken cancellationToken) =>
        GetAsync(
            () => globalMasterDataService.GetVendorTypesAsync(cancellationToken),
            "Vendor types loaded.",
            "GetVendorTypes");

    private async Task<ActionResult<ApiResponseDto<IReadOnlyList<TData>>>> GetAsync<TData>(
        Func<Task<IReadOnlyList<TData>>> load,
        string successMessage,
        string logContext)
    {
        try
        {
            var data = await load().ConfigureAwait(false);
            return Ok(new ApiResponseDto<IReadOnlyList<TData>>
            {
                Success = true,
                Message = successMessage,
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GlobalMasterData/{Context}.", logContext);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<IReadOnlyList<TData>>
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    Errors = ["INTERNAL_SERVER_ERROR"]
                });
        }
    }
}
