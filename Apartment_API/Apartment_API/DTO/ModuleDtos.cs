namespace Apartment_API.DTO;

public sealed class ModuleListItemDto
{
    public int IdModule { get; init; }
    public string ModuleCode { get; init; } = string.Empty;
    public string ModuleName { get; init; } = string.Empty;
    public string ModuleGroup { get; init; } = string.Empty;
    public string? ParentModuleCode { get; init; }
    public string? Description { get; init; }
    public string? IconCode { get; init; }
    public string? RoutePath { get; init; }
    public bool SupportsView { get; init; }
    public bool SupportsCreate { get; init; }
    public bool SupportsEdit { get; init; }
    public bool SupportsDelete { get; init; }
    public bool SupportsApprove { get; init; }
    public bool SupportsExport { get; init; }
    public short DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public bool IsSystem { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}
