namespace Apartment_API.DTO;

// --- Owners ---
public sealed class OwnerListDto
{
    public int PersonId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public IReadOnlyList<string> LinkedUnits { get; init; } = [];
    public string? IdentityDocType { get; init; }
    public int VehicleCount { get; init; }
    public IReadOnlyList<string> VehicleNumbers { get; init; } = [];
    public bool IsActive { get; init; }
}

public sealed class LinkedUnitItemDto
{
    public int UnitOwnerId { get; init; }
    public int UnitId { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public bool IsPrimaryOwner { get; init; }
    public DateTime OwnershipFromDate { get; init; }
}

public sealed class VehicleItemDto
{
    public int Id { get; init; }
    public string VehicleNumber { get; init; } = string.Empty;
    public string? Make { get; init; }
    public string? Color { get; init; }
}

public sealed class CoOwnerMinDto
{
    public int PersonId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public decimal? OwnershipShare { get; init; }
}

public sealed class OwnerDetailDto
{
    public int PersonId { get; init; }
    public string PersonNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? AlternatePhone { get; init; }
    public int? IdentityDocTypeId { get; init; }
    public string? IdentityDocNumber { get; init; }
    public string? PermanentAddress { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<LinkedUnitItemDto> LinkedUnits { get; init; } = [];
    public IReadOnlyList<VehicleItemDto> Vehicles { get; init; } = [];
    public IReadOnlyList<CoOwnerMinDto> CoOwners { get; init; } = [];
}

public sealed class VehicleRequestItem
{
    public string VehicleNumber { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Color { get; set; }
}

public sealed class CreateOwnerRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? AlternatePhone { get; set; }
    public int? IdentityDocTypeId { get; set; }
    public string? IdentityDocNumber { get; set; }
    public string? PermanentAddress { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public List<int> LinkedUnitIds { get; set; } = [];
    public DateTime OwnershipFromDate { get; set; }
    public List<VehicleRequestItem> Vehicles { get; set; } = [];
}

// --- Co-owners ---
public sealed class CoOwnerListDto
{
    public int Id { get; init; }
    public int PrimaryOwnerPersonId { get; init; }
    public string PrimaryOwnerName { get; init; } = string.Empty;
    public int UnitId { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public int CoOwnerPersonId { get; init; }
    public string CoOwnerName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public decimal? OwnershipSharePct { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CreateCoOwnerRequest
{
    public int PrimaryOwnerPersonId { get; set; }
    public int UnitId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public decimal OwnershipSharePct { get; set; }
    public int? IdentityDocTypeId { get; set; }
    public string? IdentityDocNumber { get; set; }
}

public sealed class CoOwnerCreatedDto
{
    public int Id { get; init; }
    public int PersonId { get; init; }
}

// --- Tenants ---
public sealed class TenantListDto
{
    public int Id { get; init; }
    public int PersonId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public int UnitId { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public string? OwnerName { get; init; }
    public DateTime? LeaseStartDate { get; init; }
    public DateTime? LeaseEndDate { get; init; }
    public decimal? MonthlyRent { get; init; }
    public int VehicleCount { get; init; }
    public IReadOnlyList<string> VehicleNumbers { get; init; } = [];
    public string? LeaseStatus { get; init; }
}

public sealed class CreateTenantRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public int? IdentityDocTypeId { get; set; }
    public string? IdentityDocNumber { get; set; }
    public int UnitId { get; set; }
    public DateTime LeaseStartDate { get; set; }
    public DateTime? LeaseEndDate { get; set; }
    public decimal? MonthlyRent { get; set; }
    public decimal? SecurityDeposit { get; set; }
    public List<VehicleRequestItem> Vehicles { get; set; } = [];
}

public sealed class TenantCreatedDto
{
    public int Id { get; init; }
    public int PersonId { get; init; }
}

public sealed class VacateTenantRequest
{
    public DateTime VacatedDate { get; set; }
    public string? Remarks { get; set; }
}

// --- Family ---
public sealed class FamilyMemberDto
{
    public int PersonId { get; init; }
    public int UnitId { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Relationship { get; init; }
    public int? Age { get; init; }
    public string? Gender { get; init; }
    public string? ContactNumber { get; init; }
    public string? SpecialNotes { get; init; }
}

public sealed class CreateFamilyMemberRequest
{
    public int UnitId { get; set; }
    public int ParentPersonId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Relationship { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public string? ContactNumber { get; set; }
    public string? SpecialNotes { get; set; }
}

// --- Ownership history ---
public sealed class RecordOwnershipTransferRequest
{
    public int UnitId { get; set; }
    public string TransferType { get; set; } = "Sale";
    public DateTime TransferDate { get; set; }
    public string? SaleDeedReference { get; set; }
    public int NewOwnerPersonId { get; set; }
    public decimal? TransferValue { get; set; }
    public string? Remarks { get; set; }
}

public sealed class OwnershipTransferCreatedDto
{
    public int Id { get; init; }
    public int NewPrimaryUnitOwnerId { get; init; }
}

public sealed class OwnershipHistoryItemDto
{
    public int Id { get; init; }
    public int UnitId { get; init; }
    public int? PreviousOwnerPersonId { get; init; }
    public string? PreviousOwnerName { get; init; }
    public int NewOwnerPersonId { get; init; }
    public string? NewOwnerName { get; init; }
    public string TransferType { get; init; } = string.Empty;
    public DateTime TransferDate { get; init; }
    public string? SaleDeedReference { get; init; }
    public decimal? TransferValue { get; init; }
    public string? DeedDocumentUrl { get; init; }
    public DateTime RecordedAt { get; init; }
}

public sealed class IdProofResultDto
{
    public int DocumentId { get; init; }
    public string FileUrl { get; init; } = string.Empty;
}
