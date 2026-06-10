using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class RolePermissionService(AppDbContext db) : IRolePermissionService
{
    public async Task<IReadOnlyList<RolePermissionListItemDto>> ListRolePermissionsForApartmentAsync(
        int apartmentId,
        int? roleId,
        string? moduleCode,
        CancellationToken cancellationToken = default)
    {
        var q = db.RolePermissions.AsNoTracking().Where(x => x.ApartmentId == apartmentId);
        if (roleId is { } rid)
            q = q.Where(x => x.RoleId == rid);
        if (!string.IsNullOrWhiteSpace(moduleCode))
        {
            var code = moduleCode.Trim();
            q = q.Where(x => x.ModuleCode == code);
        }

        var rows = await q
            .OrderBy(x => x.RoleId)
            .ThenBy(x => x.ModuleCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(x => new RolePermissionListItemDto
        {
            Id = x.IdRolePermission,
            ApartmentId = x.ApartmentId,
            RoleId = x.RoleId,
            ModuleCode = x.ModuleCode,
            ModuleName = ModuleCodes.GetDisplayName(x.ModuleCode),
            CanView = x.CanView,
            CanCreate = x.CanCreate,
            CanEdit = x.CanEdit,
            CanDelete = x.CanDelete,
            CanApprove = x.CanApprove,
            UpdatedAt = x.UpdatedAt,
            UpdatedBy = x.UpdatedBy
        }).ToList();
    }
}
