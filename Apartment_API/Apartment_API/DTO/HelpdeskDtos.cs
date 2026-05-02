using System.Text.Json.Serialization;

namespace Apartment_API.DTO;

public sealed class HelpDeskCategoryDto
{
    public int IdHelpDeskCategory { get; init; }
    public string HelpDeskCategoryName { get; init; } = string.Empty;
}

public sealed class HelpDeskCategoryListDto
{
    public IReadOnlyList<HelpDeskCategoryDto> Items { get; init; } = [];

    [JsonPropertyName("total")]
    public int Total { get; init; }
}

public sealed class CreateHelpDeskCategoryRequest
{
    public string HelpDeskCategoryName { get; set; } = string.Empty;
}

public sealed class UpdateHelpDeskCategoryRequest
{
    public string HelpDeskCategoryName { get; set; } = string.Empty;
}

public sealed class LogComplaintRequest
{
    public int HelpdeskCategoryID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AgreementDocUrl { get; set; }
    public int? UnitID { get; set; }
    public int? OwnerTenantID { get; set; }
}

public sealed class LogComplaintResponseDto
{
    public int IdHelpDesk { get; init; }
    public string ComplaintCode { get; init; } = string.Empty;
    public string CurrentStatus { get; init; } = string.Empty;
    public DateTime EntryDate { get; init; }
    public int FirstStatusEntryId { get; init; }
}

public sealed class ComplaintUnitDto
{
    public int UnitId { get; init; }
    public string UnitName { get; init; } = string.Empty;
    public string? Block { get; init; }
}

public sealed class ComplaintOwnerTenantDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
}

public sealed class ComplaintCategoryMiniDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class ComplaintListItemDto
{
    public int IdHelpDesk { get; init; }
    public string ComplaintCode { get; init; } = string.Empty;
    public ComplaintUnitDto Unit { get; init; } = new();
    public ComplaintOwnerTenantDto OwnerTenant { get; init; } = new();
    public ComplaintCategoryMiniDto Category { get; init; } = new();
    public string Title { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public DateTime EntryDate { get; init; }
    public string CurrentStatus { get; init; } = string.Empty;
}

public sealed class StatusUpdatedByDto
{
    public int UserId { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class HelpdeskStatusEntryDto
{
    public int Id { get; init; }
    public DateTime StatusEntryDate { get; init; }
    public string HelpdeskStatus { get; init; } = string.Empty;
    public StatusUpdatedByDto StatusUpdatedBy { get; init; } = new();
    public string StatusDetails { get; init; } = string.Empty;
    public string? AttachmentDocUrl { get; init; }
}

public sealed class ComplaintDetailDto
{
    public int IdHelpDesk { get; init; }
    public string ComplaintCode { get; init; } = string.Empty;
    public ComplaintUnitDto Unit { get; init; } = new();
    public ComplaintOwnerTenantDto OwnerTenant { get; init; } = new();
    public ComplaintCategoryMiniDto Category { get; init; } = new();
    public string Title { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? AgreementDocUrl { get; init; }
    public string CurrentStatus { get; init; } = string.Empty;
    public DateTime EntryDate { get; init; }
    public IReadOnlyList<HelpdeskStatusEntryDto> History { get; init; } = [];
}

public sealed class AppendComplaintStatusRequest
{
    public string HelpdeskStatus { get; set; } = string.Empty;
    public string StatusDetails { get; set; } = string.Empty;
    public string? AttachmentDocUrl { get; set; }
}

public sealed class AppendComplaintStatusResponseDto
{
    public int IdHelpDeskStatusDetails { get; init; }
    public int IdHelpDesk { get; init; }
    public string HelpdeskStatus { get; init; } = string.Empty;
    public DateTime StatusEntryDate { get; init; }
    public StatusUpdatedByDto StatusUpdatedBy { get; init; } = new();
    public string StatusDetails { get; init; } = string.Empty;
    public string? AttachmentDocUrl { get; init; }
    public string ComplaintCurrentStatus { get; init; } = string.Empty;
}

public sealed class HelpdeskStatsDto
{
    public int Open { get; init; }
    public int InProgress { get; init; }
    public int ResolvedThisMonth { get; init; }
    public int ClosedThisMonth { get; init; }

    [JsonPropertyName("asOf")]
    public DateOnly AsOf { get; init; }
}

public sealed class StatusSummaryRowDto
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
    public int Percent { get; init; }
}

public sealed class StatusSummaryReportDto
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public IReadOnlyList<StatusSummaryRowDto> Rows { get; init; } = [];

    [JsonPropertyName("total")]
    public int Total { get; init; }
}

public sealed class CategoryBreakdownRowDto
{
    public string Category { get; init; } = string.Empty;
    public int Logged { get; init; }
    public int Resolved { get; init; }
    public int Open { get; init; }
}

public sealed class CategoryBreakdownReportDto
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public IReadOnlyList<CategoryBreakdownRowDto> Rows { get; init; } = [];
}

public sealed class AgingBucketDto
{
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class AgingReportDto
{
    [JsonPropertyName("asOf")]
    public DateOnly AsOf { get; init; }
    public IReadOnlyList<AgingBucketDto> Buckets { get; init; } = [];
}

public sealed class HelpdeskFileUploadDto
{
    public string Url { get; init; } = string.Empty;
    public long Size { get; init; }
    public string Mime { get; init; } = string.Empty;
}
