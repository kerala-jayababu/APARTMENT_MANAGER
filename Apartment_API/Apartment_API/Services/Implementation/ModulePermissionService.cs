using Apartment_API.Configuration;
using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class ModulePermissionService(AppDbContext db) : IModulePermissionService
{
    public async Task EnsureAllowedAsync(
        int apartmentId, int roleId, string moduleCode, PermissionAction action, CancellationToken cancellationToken = default)
    {
        var code = moduleCode.Trim();
        if (!ModuleCodes.IsKnown(code))
            throw new InvalidOperationException($"Unknown module code '{code}'.");

        var rolePerms = await db.RolePermissions.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.RoleId == roleId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var perm = rolePerms.FirstOrDefault(x =>
            string.Equals(x.ModuleCode, code, StringComparison.OrdinalIgnoreCase));

        if (perm is null)
        {
            throw new ModulePermissionDeniedException(
                $"You do not have {ModuleCodes.FormatActionLabel(action)} access for module {code} ({ModuleCodes.GetDisplayName(code)}).");
        }

        var allowed = action switch
        {
            PermissionAction.View => perm.CanView,
            PermissionAction.Create => perm.CanCreate,
            PermissionAction.Edit => perm.CanEdit,
            PermissionAction.Delete => perm.CanDelete,
            PermissionAction.Approve => perm.CanApprove,
            _ => false
        };

        if (!allowed)
        {
            throw new ModulePermissionDeniedException(
                $"You do not have {ModuleCodes.FormatActionLabel(action)} access for module {code} ({ModuleCodes.GetDisplayName(code)}).");
        }
    }

    public async Task<IReadOnlyList<ModulePermissionDto>> GetPermissionsForRoleAsync(
        int apartmentId, int roleId, CancellationToken cancellationToken = default)
    {
        var rows = await db.RolePermissions.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.RoleId == roleId)
            .OrderBy(x => x.ModuleCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(x => new ModulePermissionDto
        {
            ModuleCode = x.ModuleCode,
            ModuleName = ModuleCodes.GetDisplayName(x.ModuleCode),
            CanView = x.CanView,
            CanCreate = x.CanCreate,
            CanEdit = x.CanEdit,
            CanDelete = x.CanDelete,
            CanApprove = x.CanApprove
        }).ToList();
    }
}
