namespace Apartment_API.DTO;

public sealed class RolePermissionListItemDto
{
    public int Id { get; init; }
    public int ApartmentId { get; init; }
    public int RoleId { get; init; }
    public string ModuleCode { get; init; } = string.Empty;
    public string? ModuleName { get; init; }
    public bool CanView { get; init; }
    public bool CanCreate { get; init; }
    public bool CanEdit { get; init; }
    public bool CanDelete { get; init; }
    public bool CanApprove { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int? UpdatedBy { get; init; }
}
