namespace Apartment_API.DTO;

// --- Amenity & booking ---

public sealed class AmenityTypeDto
{
    public int IdAmenityType { get; init; }
    public string AmenityTypeCode { get; init; } = string.Empty;
    public string AmenityTypeName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class BankAccountTypeDto
{
    public int IdBankAccountType { get; init; }
    public string AccountTypeCode { get; init; } = string.Empty;
    public string AccountTypeName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class BookingChargeTypeDto
{
    public int IdBookingChargeType { get; init; }
    public string ChargeTypeCode { get; init; } = string.Empty;
    public string ChargeTypeName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

// --- Committee & complaints ---

public sealed class CommitteeMemberStatusDto
{
    public int IdCommitteeMemberStatus { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CommitteeRoleDto
{
    public int IdCommitteeRole { get; init; }
    public string RoleCode { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class ComplaintCategoryDto
{
    public int IdComplaintCategory { get; init; }
    public string CategoryCode { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class MasterComplaintStatusDto
{
    public int IdComplaintStatus { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

// --- Documents, expenses, identity ---

public sealed class MasterDocumentCategoryDto
{
    public int IdDocumentCategory { get; init; }
    public string CategoryCode { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class MasterExpenseStatusDto
{
    public int IdExpenseStatus { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class IdentityDocTypeDto
{
    public int IdIdentityDocType { get; init; }
    public string DocTypeCode { get; init; } = string.Empty;
    public string DocTypeName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class MasterInvoiceStatusDto
{
    public int IdInvoiceStatus { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

// --- Notices, ownership, payments ---

public sealed class MasterNoticeCategoryDto
{
    public int IdNoticeCategory { get; init; }
    public string CategoryCode { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class OwnershipTypeDto
{
    public int IdOwnershipType { get; init; }
    public string OwnershipCode { get; init; } = string.Empty;
    public string OwnershipName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class PaymentModeDto
{
    public int IdPaymentMode { get; init; }
    public string PaymentModeCode { get; init; } = string.Empty;
    public string PaymentModeName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class PersonTypeDto
{
    public int IdPersonType { get; init; }
    public string PersonTypeCode { get; init; } = string.Empty;
    public string PersonTypeName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class PriorityLevelDto
{
    public int IdPriorityLevel { get; init; }
    public string PriorityCode { get; init; } = string.Empty;
    public string PriorityName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class MasterReconciliationStatusDto
{
    public int IdReconciliationStatus { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

// --- Security role (dbo.Roles) ---

public sealed class AppRoleListDto
{
    public int IdRole { get; init; }
    public string RoleCode { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

// --- Units ---

public sealed class MasterUnitStatusDto
{
    public int IdUnitStatus { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class UnitTypeDto
{
    public int IdUnitType { get; init; }
    public string UnitTypeCode { get; init; } = string.Empty;
    public string UnitTypeName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

// --- User list (no credentials or OTP fields) ---

public sealed class GlobalMasterUserListDto
{
    public int IdUser { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool IsSuperAdmin { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public int CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int? UpdatedBy { get; init; }
    public string? Designation { get; init; }
    public string? ProfilePhotoUrl { get; init; }
}

// --- Utility & vendor ---

public sealed class UtilityTypeDto
{
    public int IdUtilityType { get; init; }
    public string UtilityTypeCode { get; init; } = string.Empty;
    public string UtilityTypeName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class VendorTypeDto
{
    public int IdVendorType { get; init; }
    public string VendorTypeCode { get; init; } = string.Empty;
    public string VendorTypeName { get; init; } = string.Empty;
    public byte SortOrder { get; init; }
    public bool IsActive { get; init; }
}
