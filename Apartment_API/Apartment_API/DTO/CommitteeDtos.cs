namespace Apartment_API.DTO;

// --- Tenures ---
public sealed class CommitteeTenureListDto
{
    public int Id { get; init; }
    public string TenureName { get; init; } = string.Empty;
    public DateTime TenureStartDate { get; init; }
    public DateTime TenureEndDate { get; init; }
    public int MemberCount { get; init; }
    public string? PresidentName { get; init; }
    public string? SecretaryName { get; init; }
    public string? TreasurerName { get; init; }
    public bool IsActive { get; init; }
    public int DaysRemaining { get; init; }
}

public sealed class CommitteeMemberDetailItemDto
{
    public int Id { get; init; }
    public int PersonId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? UnitNumber { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public int CommitteeRoleId { get; init; }
    public string CommitteeRoleCode { get; init; } = string.Empty;
    public string CommitteeRoleName { get; init; } = string.Empty;
    public DateTime EffectiveFromDate { get; init; }
    public DateTime? EffectiveToDate { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
}

public sealed class CommitteeTenureDetailDto
{
    public int Id { get; init; }
    public string TenureName { get; init; } = string.Empty;
    public DateTime TenureStartDate { get; init; }
    public DateTime TenureEndDate { get; init; }
    public string? Notes { get; init; }
    public int DaysRemaining { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<CommitteeMemberDetailItemDto> Members { get; init; } = [];
}

public sealed class CreateCommitteeTenureRequest
{
    public DateTime TenureStartDate { get; set; }
    public DateTime TenureEndDate { get; set; }
    public string? Notes { get; set; }
}

public sealed class ExtendTenureRequest
{
    public DateTime NewEndDate { get; set; }
    public string ExtensionReason { get; set; } = string.Empty;
}

public sealed class CommitteeTenureHistoryDto
{
    public int Id { get; init; }
    public string TenureName { get; init; } = string.Empty;
    public DateTime TenureStartDate { get; init; }
    public DateTime TenureEndDate { get; init; }
    public string? PresidentName { get; init; }
    public string? SecretaryName { get; init; }
    public string? TreasurerName { get; init; }
    public int MemberCount { get; init; }
    public bool IsActive { get; init; }
}

public sealed class TenureExtensionDto
{
    public int Id { get; init; }
    public DateTime PreviousEndDate { get; init; }
    public DateTime NewEndDate { get; init; }
    public string ExtensionReason { get; init; } = string.Empty;
    public int ExtendedByUserId { get; init; }
    public string? ExtendedByName { get; init; }
    public DateTime ExtendedAt { get; init; }
}

// --- Members ---
public sealed class CommitteeMemberListDto
{
    public int Id { get; init; }
    public int CommitteeTenureId { get; init; }
    public string TenureName { get; init; } = string.Empty;
    public int PersonId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? UnitNumber { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public int CommitteeRoleId { get; init; }
    public string CommitteeRoleCode { get; init; } = string.Empty;
    public string CommitteeRoleName { get; init; } = string.Empty;
    public DateTime EffectiveFromDate { get; init; }
    public DateTime? EffectiveToDate { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
}

public sealed class AssignCommitteeMemberRequest
{
    public int CommitteeTenureId { get; set; }
    public int PersonId { get; set; }
    public int CommitteeRoleId { get; set; }
    public DateTime EffectiveFromDate { get; set; }
}

public sealed class UpdateCommitteeMemberRequest
{
    public int CommitteeRoleId { get; set; }
    public DateTime EffectiveFromDate { get; set; }
    public string? StatusCode { get; set; }
}

public sealed class EndCommitteeMemberRequest
{
    public DateTime EndDate { get; set; }
    public string EndStatusCode { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public sealed class EligibleOwnerDto
{
    public int PersonId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? UnitNumber { get; init; }
    public string? PhoneNumber { get; init; }
    public bool IsAlreadyOnTenure { get; init; }
}
