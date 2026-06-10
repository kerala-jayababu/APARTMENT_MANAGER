namespace Apartment_API.DTO;

public sealed class ModulePermissionDto
{
    public string ModuleCode { get; init; } = string.Empty;
    public string? ModuleName { get; init; }
    public bool CanView { get; init; }
    public bool CanCreate { get; init; }
    public bool CanEdit { get; init; }
    public bool CanDelete { get; init; }
    public bool CanApprove { get; init; }
}

public sealed class MyModulePermissionsDto
{
    public int ApartmentId { get; init; }
    public int RoleId { get; init; }
    public IReadOnlyList<ModulePermissionDto> Permissions { get; init; } = [];
}
