namespace Apartment_API.DTO;

public sealed class ModuleGroupListItemDto
{
    public string ModuleGroupCode { get; init; } = string.Empty;
    public string? ModuleGroupName { get; init; }
    public int? DisplayOrder { get; init; }
    public bool? IsActive { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? CreatedAt { get; init; }
}
