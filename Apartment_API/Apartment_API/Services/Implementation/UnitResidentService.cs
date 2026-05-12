using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class UnitResidentService(AppDbContext db) : IUnitResidentService
{
    public async Task<PagedResult<UnitListDto>> ListUnitsAsync(
        int apartmentId, string? search, int? blockId, int? unitTypeId, int? unitStatusId, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = db.Units.AsNoTracking().Where(u => u.ApartmentId == apartmentId && u.IsActive);
        if (blockId is { } bid) q = q.Where(u => u.BlockId == bid);
        if (unitTypeId is { } utid) q = q.Where(u => u.UnitTypeId == utid);
        if (unitStatusId is { } us) q = q.Where(u => u.UnitStatusId == us);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(u => u.UnitNumber.Contains(s) || (u.Block != null && u.Block.Contains(s)));
        }
        var total = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var pageRows = await q
            .OrderBy(u => u.UnitNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (pageRows.Count == 0)
            return new PagedResult<UnitListDto> { Items = [], TotalCount = total, Page = page, PageSize = pageSize };
        var unitIds = pageRows.Select(u => u.IdUnit).ToList();
        var types = await db.UnitTypes.AsNoTracking().ToDictionaryAsync(t => t.IdUnitType, t => t, cancellationToken)
            .ConfigureAwait(false);
        var statuses = await db.UnitStatuses.AsNoTracking()
            .ToDictionaryAsync(s => s.IdUnitStatus, s => s, cancellationToken).ConfigureAwait(false);
        var blocks = await db.Blocks.AsNoTracking()
            .Where(b => b.ApartmentId == apartmentId)
            .ToDictionaryAsync(b => b.IdBlock, b => b.BlockCode, cancellationToken)
            .ConfigureAwait(false);
        var primaryRows = await (
            from uo in db.UnitOwners.AsNoTracking()
            where uo.ApartmentId == apartmentId && uo.IsPrimaryOwner && uo.IsActive
                  && unitIds.Contains(uo.UnitId)
            join p in db.Persons.AsNoTracking() on uo.PersonId equals p.IdPerson
            select new { uo.UnitId, p.FullName }
        ).ToListAsync(cancellationToken).ConfigureAwait(false);
        var ownerByUnit = primaryRows
            .GroupBy(x => x.UnitId)
            .ToDictionary(g => g.Key, g => g.First().FullName);
        var list = new List<UnitListDto>(pageRows.Count);
        foreach (var u in pageRows)
        {
            string? bName = u.Block;
            if (u.BlockId is { } bid2 && blocks.TryGetValue(bid2, out var bc)) bName = bc;
            types.TryGetValue(u.UnitTypeId, out var ut);
            statuses.TryGetValue(u.UnitStatusId, out var st);
            ownerByUnit.TryGetValue(u.IdUnit, out var on);
            list.Add(new UnitListDto
            {
                Id = u.IdUnit,
                UnitNumber = u.UnitNumber,
                Block = bName,
                Floor = u.Floor,
                UnitType = ut?.UnitTypeName,
                CarpetArea = u.CarpetArea,
                Facing = u.Facing,
                UnitStatus = st?.StatusName,
                UnitStatusCode = st?.StatusCode,
                PrimaryOwnerName = on
            });
        }
        return new PagedResult<UnitListDto> { Items = list, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<UnitDetailDto?> GetUnitAsync(
        int apartmentId, int id, CancellationToken cancellationToken = default)
    {
        var u = await db.Units.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdUnit == id && x.ApartmentId == apartmentId, cancellationToken)
            .ConfigureAwait(false);
        if (u is null) return null;
        var ut = await db.UnitTypes.AsNoTracking().FirstOrDefaultAsync(t => t.IdUnitType == u.UnitTypeId, cancellationToken);
        var st = await db.UnitStatuses.AsNoTracking().FirstOrDefaultAsync(s => s.IdUnitStatus == u.UnitStatusId, cancellationToken);
        var ot = await db.OwnershipTypes.AsNoTracking()
            .FirstOrDefaultAsync(o => o.IdOwnershipType == u.OwnershipTypeId, cancellationToken);
        string? bCode = u.Block;
        if (u.BlockId is { } bid)
        {
            var b = await db.Blocks.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdBlock == bid && x.ApartmentId == apartmentId, cancellationToken)
                .ConfigureAwait(false);
            if (b is not null) bCode = b.BlockCode;
        }
        PersonMiniDto? primary = null;
        var uo = await db.UnitOwners.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ApartmentId == apartmentId && x.UnitId == id && x.IsPrimaryOwner && x.IsActive,
                cancellationToken)
            .ConfigureAwait(false);
        if (uo is not null)
        {
            var p = await db.Persons.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdPerson == uo.PersonId, cancellationToken)
                .ConfigureAwait(false);
            if (p is not null)
            {
                primary = new PersonMiniDto
                {
                    PersonId = p.IdPerson,
                    FullName = p.FullName,
                    PhoneNumber = p.PhoneNumber
                };
            }
        }
        CurrentMmcDto? mmc = null;
        var mrow = await db.UnitMmcDetails.AsNoTracking()
            .Where(m => m.ApartmentId == apartmentId && m.UnitId == id && m.IsActive)
            .OrderByDescending(m => m.MmcPeriodFrom)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (mrow is not null)
        {
            mmc = new CurrentMmcDto { Amount = mrow.MmcAmount, FromDate = mrow.MmcPeriodFrom };
        }
        return new UnitDetailDto
        {
            Id = u.IdUnit,
            UnitNumber = u.UnitNumber,
            BlockId = u.BlockId,
            Block = bCode,
            Floor = u.Floor,
            UnitTypeId = u.UnitTypeId,
            UnitType = ut?.UnitTypeName,
            CarpetArea = u.CarpetArea,
            BuiltUpArea = u.BuiltUpArea,
            Facing = u.Facing,
            UnitStatusId = u.UnitStatusId,
            UnitStatus = st?.StatusName,
            OwnershipTypeId = u.OwnershipTypeId,
            OwnershipType = ot?.OwnershipName,
            PrimaryOwner = primary,
            CurrentMmc = mmc,
            OpeningReceivable = u.OpeningReceivable,
            OpeningAdvance = 0,
            IsActive = u.IsActive
        };
    }

    public async Task<int> CreateUnitAsync(
        int apartmentId, int userId, CreateUnitRequest request, CancellationToken cancellationToken = default)
    {
        ValidateUnitWriteRequest(request);
        var st = await db.UnitStatuses.AsNoTracking()
            .FirstOrDefaultAsync(s => s.IdUnitStatus == request.UnitStatusId, cancellationToken)
            .ConfigureAwait(false);
        if (st is null) throw new InvalidOperationException("Invalid unitStatusId.");
        var code = (st.StatusCode ?? "").ToUpperInvariant();
        var statusName = st.StatusName ?? "";
        var impliesOwnerOccupancy =
            code is "OWNED" or "OWN"
            || (statusName.Contains("own", StringComparison.OrdinalIgnoreCase)
                && !statusName.Contains("rent", StringComparison.OrdinalIgnoreCase));
        if (impliesOwnerOccupancy && request.PrimaryOwnerPersonId is null)
            throw new InvalidOperationException(
                "This status requires primaryOwnerPersonId. Renters use tenant flow; vacant units need no owner.");
        if (request.PrimaryOwnerPersonId is { } primaryPersonId)
            await ValidatePrimaryOwnerForUnitAsync(apartmentId, primaryPersonId, cancellationToken).ConfigureAwait(false);
        var exists = await db.Units.AnyAsync(
            u => u.ApartmentId == apartmentId && u.UnitNumber == request.UnitNumber.Trim() && u.IsActive,
            cancellationToken).ConfigureAwait(false);
        if (exists) throw new InvalidOperationException("Unit number already exists for this apartment.");
        if (request.BlockId is { } bId)
        {
            var ok = await db.Blocks.AnyAsync(
                b => b.IdBlock == bId && b.ApartmentId == apartmentId && b.IsActive, cancellationToken);
            if (!ok) throw new InvalidOperationException("Invalid blockId.");
        }
        var now = DateTime.UtcNow;
        var unit = new Unit
        {
            ApartmentId = apartmentId,
            BlockId = request.BlockId,
            UnitNumber = request.UnitNumber.Trim(),
            Block = request.BlockId is { } b2
                ? (await db.Blocks.AsNoTracking().FirstAsync(x => x.IdBlock == b2, cancellationToken)).BlockCode
                : null,
            Floor = (short)request.Floor,
            UnitTypeId = request.UnitTypeId,
            CarpetArea = request.CarpetArea,
            BuiltUpArea = request.BuiltUpArea,
            Facing = string.IsNullOrWhiteSpace(request.Facing) ? null : request.Facing!.Trim(),
            UnitStatusId = request.UnitStatusId,
            OwnershipTypeId = request.OwnershipTypeId,
            IdCurrentOwner = request.PrimaryOwnerPersonId,
            CurrentMmcAmount = 0,
            OpeningReceivable = request.OpeningReceivable,
            OpeningReceivableAsOn = null,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId
        };
        db.Units.Add(unit);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (request.PrimaryOwnerPersonId is { } ownerId)
        {
            db.UnitOwners.Add(new UnitOwner
            {
                ApartmentId = apartmentId,
                UnitId = unit.IdUnit,
                PersonId = ownerId,
                IsPrimaryOwner = true,
                OwnershipFromDate = (request.PrimaryOwnershipFromDate ?? DateTime.UtcNow).Date,
                OwnershipSharePct = null,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = userId
            });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        return unit.IdUnit;
    }

    private const int FloorMin = 0;
    private const int FloorMax = 200;

    private static void ValidateUnitWriteRequest(CreateUnitRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UnitNumber))
            throw new InvalidOperationException("UnitNumber is required.");
        if (request.Floor < FloorMin || request.Floor > FloorMax)
            throw new InvalidOperationException($"floor must be between {FloorMin} and {FloorMax}.");
        if (request.CarpetArea is { } ca && ca < 0)
            throw new InvalidOperationException("carpetArea must be >= 0.");
        if (request.BuiltUpArea is { } ba && ba < 0)
            throw new InvalidOperationException("builtUpArea must be >= 0.");
        if (request.BuiltUpArea is { } b && request.CarpetArea is { } c && b < c)
            throw new InvalidOperationException("builtUpArea must be >= carpetArea when both are set.");
    }

    private async Task ValidatePrimaryOwnerForUnitAsync(
        int apartmentId, int personId, CancellationToken cancellationToken)
    {
        var row = await (
            from p in db.Persons.AsNoTracking()
            where p.IdPerson == personId && p.ApartmentId == apartmentId && p.IsActive
            join t in db.PersonTypes.AsNoTracking() on p.PersonTypeId equals t.IdPersonType
            select new { p.IdPerson, t.PersonTypeCode }
        ).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (row is null)
            throw new InvalidOperationException("primaryOwnerPersonId was not found or is inactive in this apartment.");
        var normalized = (row.PersonTypeCode ?? "").Trim()
            .Replace("-", "_", StringComparison.Ordinal).ToUpperInvariant();
        if (normalized is not ("OWNER" or "CO_OWNER"))
            throw new InvalidOperationException(
                $"Person {row.IdPerson} must have person type OWNER or CO-OWNER to be set as primary owner.");
    }

    public async Task UpdateUnitAsync(
        int apartmentId, int userId, int id, CreateUnitRequest request, CancellationToken cancellationToken = default)
    {
        ValidateUnitWriteRequest(request);
        var u = await db.Units.FirstOrDefaultAsync(
            x => x.IdUnit == id && x.ApartmentId == apartmentId, cancellationToken).ConfigureAwait(false);
        if (u is null) throw new InvalidOperationException("Unit not found.");
        u.BlockId = request.BlockId;
        u.UnitNumber = request.UnitNumber.Trim();
        u.Floor = (short)request.Floor;
        u.UnitTypeId = request.UnitTypeId;
        u.CarpetArea = request.CarpetArea;
        u.BuiltUpArea = request.BuiltUpArea;
        u.Facing = string.IsNullOrWhiteSpace(request.Facing) ? null : request.Facing!.Trim();
        u.UnitStatusId = request.UnitStatusId;
        u.OwnershipTypeId = request.OwnershipTypeId;
        u.IdCurrentOwner = request.PrimaryOwnerPersonId;
        u.UpdatedAt = DateTime.UtcNow;
        u.UpdatedBy = userId;
        if (request.BlockId is { } b2)
        {
            u.Block = (await db.Blocks.AsNoTracking()
                .FirstAsync(x => x.IdBlock == b2, cancellationToken)).BlockCode;
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<BulkGenerateResultDto> BulkGenerateAsync(
        int apartmentId, int userId, BulkGenerateUnitsRequest request, CancellationToken cancellationToken = default)
    {
        var b = await db.Blocks.FirstOrDefaultAsync(
            x => x.IdBlock == request.BlockId && x.ApartmentId == apartmentId, cancellationToken);
        if (b is null) throw new InvalidOperationException("Block not found.");
        var now = DateTime.UtcNow;
        var defaultOwnership = await db.OwnershipTypes.AsNoTracking()
            .OrderBy(o => o.IdOwnershipType)
            .Select(o => o.IdOwnershipType)
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);
        var created = 0;
        var skipped = 0;
        for (var fl = 0; fl < request.Floors; fl++)
        {
            var floorNum = request.StartingFloor + fl;
            for (var n = 1; n <= request.UnitsPerFloor; n++)
            {
                var num = request.UnitNumberPrefix + floorNum + n.ToString("D2");
                var exists = await db.Units.AnyAsync(
                    u => u.ApartmentId == apartmentId && u.UnitNumber == num, cancellationToken);
                if (exists) { skipped++; continue; }
                db.Units.Add(new Unit
                {
                    ApartmentId = apartmentId,
                    BlockId = request.BlockId,
                    UnitNumber = num,
                    Block = b.BlockCode,
                    Floor = (short)floorNum,
                    UnitTypeId = request.DefaultUnitTypeId,
                    CarpetArea = request.DefaultCarpetArea,
                    UnitStatusId = request.InitialUnitStatusId,
                    OwnershipTypeId = defaultOwnership,
                    CurrentMmcAmount = 0,
                    OpeningReceivable = 0,
                    OpeningReceivableAsOn = null,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = userId
                });
                created++;
            }
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new BulkGenerateResultDto { CreatedCount = created, SkippedExisting = skipped };
    }

    public async Task<IReadOnlyList<BlockOccupancyDto>> GetOccupancyAsync(
        int apartmentId, int? blockId, CancellationToken cancellationToken = default)
    {
        var bq = db.Blocks.AsNoTracking().Where(b => b.ApartmentId == apartmentId && b.IsActive);
        if (blockId is { } x) bq = bq.Where(b => b.IdBlock == x);
        var blks = await bq.OrderBy(b => b.BlockCode).ToListAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<BlockOccupancyDto>(blks.Count);
        var statuses = await db.UnitStatuses.AsNoTracking()
            .ToDictionaryAsync(s => s.IdUnitStatus, s => s.StatusCode, cancellationToken)
            .ConfigureAwait(false);
        foreach (var b in blks)
        {
            var units = await db.Units.AsNoTracking()
                .Where(u => u.ApartmentId == apartmentId && u.BlockId == b.IdBlock && u.IsActive)
                .OrderBy(u => u.Floor).ThenBy(u => u.UnitNumber)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var udtos = units.Select(u => new BlockOccupancyUnitDto
            {
                UnitId = u.IdUnit,
                UnitNumber = u.UnitNumber,
                Floor = u.Floor,
                StatusCode = statuses.TryGetValue(u.UnitStatusId, out var sc) ? sc : null
            }).ToList();
            result.Add(new BlockOccupancyDto
            {
                BlockId = b.IdBlock,
                BlockCode = b.BlockCode,
                TotalFloors = b.TotalFloors,
                Units = udtos
            });
        }
        return result;
    }

    public async Task<int> ChangeStatusAsync(
        int apartmentId, int userId, int unitId, ChangeUnitStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 300)
            throw new InvalidOperationException("Reason is required and must be 300 characters or less.");
        var u = await db.Units.FirstOrDefaultAsync(
            x => x.IdUnit == unitId && x.ApartmentId == apartmentId, cancellationToken);
        if (u is null) throw new InvalidOperationException("Unit not found.");
        var prev = u.UnitStatusId;
        u.UnitStatusId = request.NewStatusId;
        u.UpdatedAt = DateTime.UtcNow;
        u.UpdatedBy = userId;
        var now = DateTime.UtcNow;
        var h = new UnitStatusHistory
        {
            ApartmentId = apartmentId,
            UnitId = unitId,
            PreviousStatusId = prev,
            NewStatusId = request.NewStatusId,
            EffectiveDate = request.EffectiveDate.Date,
            LinkedPersonId = request.LinkedPersonId,
            Reason = request.Reason.Trim(),
            ChangedByUserId = userId,
            ChangedAt = now
        };
        db.UnitStatusHistory.Add(h);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return h.IdUnitStatusHistory;
    }

    public async Task<PagedResult<UnitStatusHistoryDto>> GetStatusHistoryAsync(
        int apartmentId, int? unitId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q2 = db.UnitStatusHistory.AsNoTracking().Where(h => h.ApartmentId == apartmentId);
        if (unitId is { } uidFilter) q2 = q2.Where(h => h.UnitId == uidFilter);
        if (from is { } f2) q2 = q2.Where(x => x.EffectiveDate >= f2.Date);
        if (to is { } t2) q2 = q2.Where(x => x.EffectiveDate <= t2.Date);
        var total = await q2.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await q2
            .OrderByDescending(x => x.ChangedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (rows.Count == 0)
            return new PagedResult<UnitStatusHistoryDto> { Items = [], TotalCount = total, Page = page, PageSize = pageSize };
        var items = new List<UnitStatusHistoryDto>(rows.Count);
        foreach (var h in rows)
        {
            var unitRow = await db.Units.AsNoTracking().FirstAsync(x => x.IdUnit == h.UnitId, cancellationToken);
            var pSt = await db.UnitStatuses.AsNoTracking().FirstAsync(s => s.IdUnitStatus == h.PreviousStatusId, cancellationToken);
            var nSt = await db.UnitStatuses.AsNoTracking().FirstAsync(s => s.IdUnitStatus == h.NewStatusId, cancellationToken);
            string? bCode = null;
            if (unitRow.BlockId is { } bi)
            {
                var bl = await db.Blocks.AsNoTracking().FirstOrDefaultAsync(b => b.IdBlock == bi, cancellationToken);
                bCode = bl?.BlockCode;
            }
            var changedBy = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdUser == h.ChangedByUserId, cancellationToken);
            string? lpName = null;
            string? lpRole = null;
            if (h.LinkedPersonId is { } lpid)
            {
                var lp = await db.Persons.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdPerson == lpid, cancellationToken)
                    .ConfigureAwait(false);
                lpName = lp?.FullName;
                lpRole = await ResolveLinkedPersonRoleAsync(apartmentId, h.UnitId, lpid, cancellationToken)
                    .ConfigureAwait(false);
            }
            items.Add(new UnitStatusHistoryDto
            {
                Id = h.IdUnitStatusHistory,
                UnitId = h.UnitId,
                UnitNumber = unitRow.UnitNumber,
                Block = bCode,
                PreviousStatus = pSt.StatusName,
                NewStatus = nSt.StatusName,
                EffectiveDate = h.EffectiveDate,
                LinkedPersonName = lpName,
                LinkedPersonRole = lpRole,
                Reason = h.Reason,
                ChangedByName = changedBy?.FullName,
                ChangedAt = h.ChangedAt
            });
        }
        return new PagedResult<UnitStatusHistoryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<IReadOnlyList<UnitStatusHistoryDto>> GetStatusHistoryForUnitAsync(
        int apartmentId, int unitId, CancellationToken cancellationToken = default)
    {
        var r = await GetStatusHistoryAsync(apartmentId, unitId, null, null, 1, 500, cancellationToken)
            .ConfigureAwait(false);
        return r.Items.ToList();
    }

    /// <summary>Label for "Name (Role)" in status history: unit-scoped owner/tenant, else person type name.</summary>
    private async Task<string?> ResolveLinkedPersonRoleAsync(
        int apartmentId, int unitId, int personId, CancellationToken cancellationToken)
    {
        var personTypeRow = await (
            from p in db.Persons.AsNoTracking()
            where p.IdPerson == personId && p.ApartmentId == apartmentId
            join pt in db.PersonTypes.AsNoTracking() on p.PersonTypeId equals pt.IdPersonType
            select new { pt.PersonTypeCode, pt.PersonTypeName }
        ).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (personTypeRow is null) return null;

        var isUnitOwner = await db.UnitOwners.AsNoTracking().AnyAsync(
            uo => uo.ApartmentId == apartmentId && uo.UnitId == unitId && uo.PersonId == personId && uo.IsActive,
            cancellationToken).ConfigureAwait(false);
        if (isUnitOwner)
        {
            if (string.Equals(personTypeRow.PersonTypeCode, PersonTypeCodes.CoOwner, StringComparison.OrdinalIgnoreCase))
                return "Co-owner";
            return "Owner";
        }

        var isTenant = await db.TenantAssignments.AsNoTracking().AnyAsync(
            ta => ta.ApartmentId == apartmentId && ta.UnitId == unitId && ta.PersonId == personId && ta.IsActive,
            cancellationToken).ConfigureAwait(false);
        if (isTenant) return "Tenant";

        return string.IsNullOrWhiteSpace(personTypeRow.PersonTypeName) ? null : personTypeRow.PersonTypeName;
    }
}
