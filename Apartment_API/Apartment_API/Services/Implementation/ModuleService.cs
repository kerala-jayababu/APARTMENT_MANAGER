using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class ModuleService(AppDbContext db) : IModuleService
{
    public async Task<IReadOnlyList<ModuleListItemDto>> ListModulesAsync(
        bool? isActive,
        string? moduleGroup,
        string? parentModuleCode,
        CancellationToken cancellationToken = default)
    {
        var q = db.AppModules.AsNoTracking().AsQueryable();
        if (isActive is { } active)
            q = q.Where(x => x.IsActive == active);
        if (!string.IsNullOrWhiteSpace(moduleGroup))
            q = q.Where(x => x.ModuleGroup == moduleGroup.Trim());
        if (!string.IsNullOrWhiteSpace(parentModuleCode))
            q = q.Where(x => x.ParentModuleCode == parentModuleCode.Trim());

        return await q
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ModuleName)
            .Select(x => new ModuleListItemDto
            {
                IdModule = x.IdModule,
                ModuleCode = x.ModuleCode,
                ModuleName = x.ModuleName,
                ModuleGroup = x.ModuleGroup,
                ParentModuleCode = x.ParentModuleCode,
                Description = x.Description,
                IconCode = x.IconCode,
                RoutePath = x.RoutePath,
                SupportsView = x.SupportsView,
                SupportsCreate = x.SupportsCreate,
                SupportsEdit = x.SupportsEdit,
                SupportsDelete = x.SupportsDelete,
                SupportsApprove = x.SupportsApprove,
                SupportsExport = x.SupportsExport,
                DisplayOrder = x.DisplayOrder,
                IsActive = x.IsActive,
                IsSystem = x.IsSystem,
                CreatedAt = x.CreatedAt,
                ModifiedAt = x.ModifiedAt
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
