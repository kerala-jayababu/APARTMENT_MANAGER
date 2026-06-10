using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class ModuleGroupService(AppDbContext db) : IModuleGroupService
{
    public async Task<IReadOnlyList<ModuleGroupListItemDto>> ListModuleGroupsAsync(
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var q = db.AppModuleGroups.AsNoTracking().AsQueryable();
        if (isActive is { } active)
        {
            var flag = active ? "1" : "0";
            q = q.Where(x => x.IsActive == flag || x.IsActive == (active ? "Y" : "N"));
        }

        var rows = await q
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ModuleGroupName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(x => new ModuleGroupListItemDto
        {
            ModuleGroupCode = x.ModuleGroupCode,
            ModuleGroupName = x.ModuleGroupName,
            DisplayOrder = x.DisplayOrder,
            IsActive = ParseIsActive(x.IsActive),
            CreatedBy = x.CreatedBy,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    private static bool? ParseIsActive(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return value.Trim() is "1" or "Y" or "y" or "T" or "t";
    }
}
