using Apartment_API.Configuration;
using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IModulePermissionService
{
    Task EnsureAllowedAsync(
        int apartmentId, int roleId, string moduleCode, PermissionAction action, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModulePermissionDto>> GetPermissionsForRoleAsync(
        int apartmentId, int roleId, CancellationToken cancellationToken = default);
}
