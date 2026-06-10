using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IRolePermissionService
{
    Task<IReadOnlyList<RolePermissionListItemDto>> ListRolePermissionsForApartmentAsync(
        int apartmentId,
        int? roleId,
        string? moduleCode,
        CancellationToken cancellationToken = default);
}
