using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class BlockService(AppDbContext db) : IBlockService
{
    private const int BlockNameMaxLength = 20;
    private const int DescriptionMaxLength = 200;

    public async Task<IReadOnlyList<BlockListDto>> ListAsync(
        int apartmentId, string? search, bool? isActive, CancellationToken cancellationToken = default)
    {
        var q = db.Blocks.AsNoTracking().Where(b => b.ApartmentId == apartmentId);
        if (isActive == true) q = q.Where(b => b.IsActive);
        else if (isActive == false) q = q.Where(b => !b.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(b =>
                (b.BlockCode != null && b.BlockCode.Contains(s)) ||
                (b.BlockName != null && b.BlockName.Contains(s)));
        }
        var list = await q.OrderBy(b => b.BlockName).ThenBy(b => b.IdBlock).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (list.Count == 0) return [];

        // Avoid ToUpper() on possibly-null DB strings; vacant list may be empty if master has no VACANT row.
        var vacantIdList = await db.UnitStatuses.AsNoTracking()
            .Where(us =>
                (us.StatusCode != null && us.StatusCode.ToUpper() == "VACANT") ||
                (us.StatusName != null && us.StatusName.ToUpper().Contains("VACANT")))
            .Select(us => us.IdUnitStatus)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new List<BlockListDto>(list.Count);
        foreach (var b in list)
        {
            var totalActive = await db.Units.AsNoTracking()
                .CountAsync(
                    u => u.ApartmentId == apartmentId && u.BlockId == b.IdBlock && u.IsActive,
                    cancellationToken)
                .ConfigureAwait(false);
            var vacant = 0;
            if (vacantIdList.Count > 0)
            {
                vacant = await db.Units.AsNoTracking()
                    .CountAsync(
                        u => u.ApartmentId == apartmentId && u.BlockId == b.IdBlock && u.IsActive
                             && vacantIdList.Contains(u.UnitStatusId),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            var occ = totalActive - vacant;
            if (totalActive == 0)
            {
                occ = 0;
                vacant = 0;
            }
            result.Add(new BlockListDto
            {
                Id = b.IdBlock,
                BlockName = b.BlockName,
                TotalFloors = b.TotalFloors,
                TotalUnits = b.TotalUnits,
                OccupiedUnits = occ,
                VacantUnits = vacant,
                Description = b.Description,
                IsActive = b.IsActive
            });
        }
        return result;
    }

    public async Task<BlockListDto?> GetByIdAsync(
        int apartmentId, int id, CancellationToken cancellationToken = default)
    {
        var all = await ListAsync(apartmentId, null, null, cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(x => x.Id == id);
    }

    public async Task<int> CreateAsync(
        int apartmentId, int userId, CreateBlockRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BlockName))
            throw new InvalidOperationException("BlockName is required.");
        if (request.TotalFloors is < 1 or > 100) throw new InvalidOperationException("totalFloors must be 1-100.");
        if (request.TotalUnits is < 1 or > 500) throw new InvalidOperationException("totalUnits must be 1-500.");
        var name = request.BlockName.Trim();
        if (name.Length is 0 or > BlockNameMaxLength)
            throw new InvalidOperationException($"BlockName must be 1–{BlockNameMaxLength} characters.");
        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > DescriptionMaxLength)
            throw new InvalidOperationException($"Description must be at most {DescriptionMaxLength} characters.");
        var nameTaken = await db.Blocks.AnyAsync(
            b => b.ApartmentId == apartmentId && b.IsActive
                 && (b.BlockName == name || b.BlockCode == name),
            cancellationToken).ConfigureAwait(false);
        if (nameTaken) throw new InvalidOperationException("A block with this name already exists in the apartment.");
        var e = new Block
        {
            ApartmentId = apartmentId,
            // Legacy column: keep in sync with display name (single M02 field).
            BlockCode = name,
            BlockName = name,
            TotalFloors = (byte)request.TotalFloors,
            TotalUnits = (short)request.TotalUnits,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description!.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };
        db.Blocks.Add(e);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return e.IdBlock;
    }

    public async Task UpdateAsync(
        int apartmentId, int userId, int id, CreateBlockRequest request, CancellationToken cancellationToken = default)
    {
        var b = await db.Blocks.FirstOrDefaultAsync(
            x => x.IdBlock == id && x.ApartmentId == apartmentId, cancellationToken).ConfigureAwait(false);
        if (b is null) throw new InvalidOperationException("Block not found.");
        if (string.IsNullOrWhiteSpace(request.BlockName))
            throw new InvalidOperationException("BlockName is required.");
        if (request.TotalFloors is < 1 or > 100) throw new InvalidOperationException("totalFloors must be 1-100.");
        if (request.TotalUnits is < 1 or > 500) throw new InvalidOperationException("totalUnits must be 1-500.");
        var name = request.BlockName.Trim();
        if (name.Length is 0 or > BlockNameMaxLength)
            throw new InvalidOperationException($"BlockName must be 1–{BlockNameMaxLength} characters.");
        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > DescriptionMaxLength)
            throw new InvalidOperationException($"Description must be at most {DescriptionMaxLength} characters.");
        var nameTaken = await db.Blocks.AnyAsync(
            x => x.ApartmentId == apartmentId && x.IsActive && x.IdBlock != id
                 && (x.BlockName == name || x.BlockCode == name),
            cancellationToken).ConfigureAwait(false);
        if (nameTaken) throw new InvalidOperationException("A block with this name already exists in the apartment.");
        b.BlockCode = name;
        b.BlockName = name;
        b.TotalFloors = (byte)request.TotalFloors;
        b.TotalUnits = (short)request.TotalUnits;
        b.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description!.Trim();
        b.UpdatedAt = DateTime.UtcNow;
        b.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int apartmentId, int id, CancellationToken cancellationToken = default)
    {
        var b = await db.Blocks.FirstOrDefaultAsync(
            x => x.IdBlock == id && x.ApartmentId == apartmentId, cancellationToken).ConfigureAwait(false);
        if (b is null) throw new InvalidOperationException("Block not found.");
        var hasUnits = await db.Units.AnyAsync(
            u => u.ApartmentId == apartmentId && u.BlockId == id && u.IsActive,
            cancellationToken).ConfigureAwait(false);
        if (hasUnits) throw new InvalidOperationException("Block has active units. Cannot delete.");
        b.IsActive = false;
        b.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
