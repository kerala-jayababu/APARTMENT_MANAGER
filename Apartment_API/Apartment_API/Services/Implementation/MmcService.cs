using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class MmcService(AppDbContext db) : IMmcService
{
    private const string BatchPending = "PENDING";
    private const string BatchApproved = "APPROVED";
    private const string BatchRejected = "REJECTED";

    public async Task<IReadOnlyList<MmcPeriodDto>> ListPeriodsAsync(
        int apartmentId, bool? isActive, CancellationToken cancellationToken = default)
    {
        var q = db.MmcPeriods.AsNoTracking().Where(x => x.ApartmentId == apartmentId);
        if (isActive is { } a) q = q.Where(x => x.IsActive == a);
        var list = await q.OrderByDescending(x => x.StartDate).ToListAsync(cancellationToken).ConfigureAwait(false);
        return list.Select(MapPeriod).ToList();
    }

    public async Task<MmcPeriodDto?> GetCurrentPeriodAsync(int apartmentId, CancellationToken cancellationToken = default)
    {
        var p = await db.MmcPeriods.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IsCurrent && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        return p is null ? null : MapPeriod(p);
    }

    public async Task<int> CreatePeriodAsync(
        int apartmentId, int userId, CreateMmcPeriodRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.PeriodCode.Trim();
        var name = request.PeriodName.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("periodCode and periodName are required.");
        if (request.EndDate.Date <= request.StartDate.Date)
            throw new InvalidOperationException("endDate must be after startDate.");
        var duplicate = await db.MmcPeriods.AsNoTracking()
            .AnyAsync(x => x.ApartmentId == apartmentId && x.PeriodCode == code, cancellationToken)
            .ConfigureAwait(false);
        if (duplicate) throw new InvalidOperationException("periodCode already exists.");

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        if (request.IsCurrent)
        {
            var currents = await db.MmcPeriods
                .Where(x => x.ApartmentId == apartmentId && x.IsCurrent)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var c in currents)
            {
                c.IsCurrent = false;
                c.UpdatedAt = now;
                c.UpdatedBy = userId;
            }
        }

        var row = new MmcPeriod
        {
            ApartmentId = apartmentId,
            PeriodCode = code,
            PeriodName = name,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate.Date,
            IsCurrent = request.IsCurrent,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId
        };
        db.MmcPeriods.Add(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return row.IdMmcPeriod;
    }

    public async Task<MmcGridDto> GetGridAsync(
        int apartmentId, int mmcPeriodId, string? search, int? blockId, int? unitTypeId, CancellationToken cancellationToken = default)
    {
        var period = await RequirePeriodAsync(apartmentId, mmcPeriodId, cancellationToken).ConfigureAwait(false);
        return await BuildGridAsync(apartmentId, period, search, blockId, unitTypeId, null, null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MmcBatchCreatedDto> SubmitBatchAsync(
        int apartmentId, int userId, SubmitMmcBatchRequest request, CancellationToken cancellationToken = default)
    {
        var period = await RequirePeriodAsync(apartmentId, request.MmcPeriodId, cancellationToken).ConfigureAwait(false);
        var lines = request.Lines ?? [];
        if (lines.Count == 0) throw new InvalidOperationException("lines are required.");
        if (lines.Any(x => x.NewMmcAmount < 0 || x.NewMmcAmount > 100000))
            throw new InvalidOperationException("newMMCAmount must be between 0 and 100000.");
        var dup = lines.GroupBy(x => x.UnitId).FirstOrDefault(g => g.Count() > 1);
        if (dup is not null) throw new InvalidOperationException("Each unitId must be unique in request.");

        var pendingExists = await db.MmcBatches.AsNoTracking()
            .AnyAsync(x => x.ApartmentId == apartmentId && x.MmcPeriodId == request.MmcPeriodId && x.StatusCode == BatchPending && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (pendingExists) throw new InvalidOperationException("A pending batch already exists for this period.");

        var unitIds = lines.Select(x => x.UnitId).ToList();
        var activeUnits = await db.Units.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.IsActive && unitIds.Contains(x.IdUnit))
            .Select(x => x.IdUnit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (activeUnits.Count != unitIds.Count) throw new InvalidOperationException("One or more units are invalid/inactive.");

        var previousByUnit = await ResolveCurrentMmcByUnitAsync(apartmentId, request.MmcPeriodId, unitIds, cancellationToken).ConfigureAwait(false);
        var anyDelta = lines.Any(x => x.NewMmcAmount != previousByUnit.GetValueOrDefault(x.UnitId));
        if (!anyDelta) throw new InvalidOperationException("No changes to approve.");

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        var batch = new MmcBatch
        {
            ApartmentId = apartmentId,
            MmcPeriodId = period.IdMmcPeriod,
            StatusCode = BatchPending,
            Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks!.Trim(),
            SubmittedByUserId = userId,
            SubmittedAt = now,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId
        };
        db.MmcBatches.Add(batch);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var line in lines)
        {
            db.MmcBatchLines.Add(new MmcBatchLine
            {
                MmcBatchId = batch.IdMmcBatch,
                UnitId = line.UnitId,
                PreviousMmcAmount = previousByUnit.GetValueOrDefault(line.UnitId),
                NewMmcAmount = line.NewMmcAmount
            });
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new MmcBatchCreatedDto { Id = batch.IdMmcBatch, LineCount = lines.Count };
    }

    public async Task<PagedResult<MmcBatchListDto>> ListBatchesAsync(
        int apartmentId, int? mmcPeriodId, string? statusCode, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = db.MmcBatches.AsNoTracking().Where(x => x.ApartmentId == apartmentId && x.IsActive);
        if (mmcPeriodId is { } p) q = q.Where(x => x.MmcPeriodId == p);
        if (!string.IsNullOrWhiteSpace(statusCode))
        {
            var s = statusCode.Trim().ToUpperInvariant();
            q = q.Where(x => x.StatusCode.ToUpper() == s);
        }
        var total = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var batches = await q.OrderByDescending(x => x.SubmittedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (batches.Count == 0) return new PagedResult<MmcBatchListDto> { Items = [], TotalCount = total, Page = page, PageSize = pageSize };

        var periodIds = batches.Select(x => x.MmcPeriodId).Distinct().ToList();
        var periods = await db.MmcPeriods.AsNoTracking().Where(x => periodIds.Contains(x.IdMmcPeriod))
            .ToDictionaryAsync(x => x.IdMmcPeriod, x => x.PeriodName, cancellationToken).ConfigureAwait(false);
        var userIds = batches.SelectMany(x => new[] { x.SubmittedByUserId, x.ApprovedByUserId }).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var users = await db.Users.AsNoTracking().Where(x => userIds.Contains(x.IdUser))
            .ToDictionaryAsync(x => x.IdUser, x => x.FullName, cancellationToken).ConfigureAwait(false);
        var batchIds = batches.Select(x => x.IdMmcBatch).ToList();
        var lines = await db.MmcBatchLines.AsNoTracking().Where(x => batchIds.Contains(x.MmcBatchId)).ToListAsync(cancellationToken).ConfigureAwait(false);
        var items = batches.Select(b =>
        {
            var bl = lines.Where(x => x.MmcBatchId == b.IdMmcBatch).ToList();
            return new MmcBatchListDto
            {
                Id = b.IdMmcBatch,
                MmcPeriodId = b.MmcPeriodId,
                PeriodName = periods.GetValueOrDefault(b.MmcPeriodId) ?? string.Empty,
                StatusCode = b.StatusCode,
                LineCount = bl.Count,
                TotalNewMonthlyMmc = bl.Sum(x => x.NewMmcAmount),
                TotalDelta = bl.Sum(x => x.NewMmcAmount - x.PreviousMmcAmount),
                SubmittedByName = users.GetValueOrDefault(b.SubmittedByUserId),
                SubmittedAt = b.SubmittedAt,
                ApprovedByName = b.ApprovedByUserId is { } au ? users.GetValueOrDefault(au) : null,
                ApprovedAt = b.ApprovedAt
            };
        }).ToList();
        return new PagedResult<MmcBatchListDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<MmcBatchDetailDto?> GetBatchAsync(int apartmentId, int id, CancellationToken cancellationToken = default)
    {
        var b = await db.MmcBatches.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdMmcBatch == id && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (b is null) return null;
        var lines = await db.MmcBatchLines.AsNoTracking().Where(x => x.MmcBatchId == id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var units = await db.Units.AsNoTracking().Where(x => x.ApartmentId == apartmentId && lines.Select(l => l.UnitId).Contains(x.IdUnit))
            .ToDictionaryAsync(x => x.IdUnit, x => x, cancellationToken).ConfigureAwait(false);
        var ownerNames = await LoadPrimaryOwnersByUnitAsync(apartmentId, lines.Select(x => x.UnitId).ToList(), cancellationToken).ConfigureAwait(false);
        var periodName = await db.MmcPeriods.AsNoTracking().Where(x => x.IdMmcPeriod == b.MmcPeriodId).Select(x => x.PeriodName)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
        var submittedBy = await db.Users.AsNoTracking().Where(x => x.IdUser == b.SubmittedByUserId).Select(x => x.FullName)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var outLines = lines.Select(l =>
        {
            var u = units.GetValueOrDefault(l.UnitId);
            return new MmcBatchDetailLineDto
            {
                UnitId = l.UnitId,
                Block = u?.Block,
                UnitNumber = u?.UnitNumber ?? string.Empty,
                PrimaryOwnerName = ownerNames.GetValueOrDefault(l.UnitId),
                PreviousMmcAmount = l.PreviousMmcAmount,
                NewMmcAmount = l.NewMmcAmount,
                DeltaAmount = l.NewMmcAmount - l.PreviousMmcAmount
            };
        }).OrderBy(x => x.UnitNumber).ToList();
        return new MmcBatchDetailDto
        {
            Id = b.IdMmcBatch,
            MmcPeriodId = b.MmcPeriodId,
            PeriodName = periodName,
            StatusCode = b.StatusCode,
            Remarks = b.Remarks,
            Lines = outLines,
            Totals = new MmcBatchDetailTotalsDto
            {
                PreviousMonthlyTotal = outLines.Sum(x => x.PreviousMmcAmount),
                NewMonthlyTotal = outLines.Sum(x => x.NewMmcAmount),
                DeltaTotal = outLines.Sum(x => x.DeltaAmount)
            },
            SubmittedByName = submittedBy,
            SubmittedAt = b.SubmittedAt
        };
    }

    public async Task ApproveBatchAsync(int apartmentId, int userId, int id, CancellationToken cancellationToken = default)
    {
        var b = await db.MmcBatches
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdMmcBatch == id && x.IsActive, cancellationToken)
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Batch not found.");
        if (!string.Equals(b.StatusCode, BatchPending, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only pending batches can be approved.");
        var period = await RequirePeriodAsync(apartmentId, b.MmcPeriodId, cancellationToken).ConfigureAwait(false);
        var lines = await db.MmcBatchLines.Where(x => x.MmcBatchId == id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        foreach (var l in lines)
        {
            var detail = await db.UnitMmcDetails
                .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.UnitId == l.UnitId && x.IdMmcPeriod == period.IdMmcPeriod, cancellationToken)
                .ConfigureAwait(false);
            if (detail is null)
            {
                var incomeHeadId = await ResolveIncomeHeadIdAsync(apartmentId, l.UnitId, cancellationToken).ConfigureAwait(false);
                detail = new UnitMmcDetail
                {
                    ApartmentId = apartmentId,
                    UnitId = l.UnitId,
                    IdMmcPeriod = period.IdMmcPeriod,
                    IncomeHeadId = incomeHeadId,
                    MmcAmount = l.NewMmcAmount,
                    MmcPeriodFrom = period.StartDate.Date,
                    MmcPeriodTo = period.EndDate.Date,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = userId
                };
                db.UnitMmcDetails.Add(detail);
            }
            else
            {
                detail.MmcAmount = l.NewMmcAmount;
                detail.UpdatedAt = now;
                detail.UpdatedBy = userId;
                detail.IsActive = true;
            }

            if (period.IsCurrent)
            {
                var unit = await db.Units.FirstAsync(x => x.ApartmentId == apartmentId && x.IdUnit == l.UnitId, cancellationToken).ConfigureAwait(false);
                unit.CurrentMmcAmount = l.NewMmcAmount;
                unit.UpdatedAt = now;
                unit.UpdatedBy = userId;
            }
        }

        b.StatusCode = BatchApproved;
        b.ApprovedByUserId = userId;
        b.ApprovedAt = now;
        b.UpdatedAt = now;
        b.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RejectBatchAsync(int apartmentId, int userId, int id, RejectRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("reason is required.");
        var b = await db.MmcBatches
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdMmcBatch == id && x.IsActive, cancellationToken)
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Batch not found.");
        if (!string.Equals(b.StatusCode, BatchPending, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only pending batches can be rejected.");
        b.StatusCode = BatchRejected;
        b.RejectionReason = request.Reason.Trim();
        b.RejectedByUserId = userId;
        b.RejectedAt = DateTime.UtcNow;
        b.UpdatedAt = DateTime.UtcNow;
        b.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<MmcGridDto> CopyFromPeriodAsync(
        int apartmentId, int mmcPeriodId, int sourcePeriodId, string? search, int? blockId, int? unitTypeId,
        CancellationToken cancellationToken = default)
    {
        var period = await RequirePeriodAsync(apartmentId, mmcPeriodId, cancellationToken).ConfigureAwait(false);
        _ = await RequirePeriodAsync(apartmentId, sourcePeriodId, cancellationToken).ConfigureAwait(false);
        var sourceRows = await db.UnitMmcDetails.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.IdMmcPeriod == sourcePeriodId)
            .ToDictionaryAsync(x => x.UnitId, x => (decimal?)x.MmcAmount, cancellationToken).ConfigureAwait(false);
        return await BuildGridAsync(apartmentId, period, search, blockId, unitTypeId, null, sourceRows, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UnitMmcHistoryDto>> GetUnitHistoryAsync(int apartmentId, int unitId, CancellationToken cancellationToken = default)
    {
        var rows = await db.UnitMmcDetails.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.UnitId == unitId)
            .OrderByDescending(x => x.MmcPeriodFrom)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (rows.Count == 0) return [];
        var periodIds = rows.Where(x => x.IdMmcPeriod.HasValue).Select(x => x.IdMmcPeriod!.Value).Distinct().ToList();
        var periods = await db.MmcPeriods.AsNoTracking()
            .Where(x => periodIds.Contains(x.IdMmcPeriod))
            .ToDictionaryAsync(x => x.IdMmcPeriod, x => x.PeriodName, cancellationToken).ConfigureAwait(false);
        var userIds = rows.Select(x => x.CreatedBy).Distinct().ToList();
        var users = await db.Users.AsNoTracking().Where(x => userIds.Contains(x.IdUser))
            .ToDictionaryAsync(x => x.IdUser, x => x.FullName, cancellationToken).ConfigureAwait(false);
        return rows.Select(x => new UnitMmcHistoryDto
        {
            IdUnitMmcDetail = x.IdUnitMmcDetail,
            MmcPeriodId = x.IdMmcPeriod,
            PeriodName = x.IdMmcPeriod is { } pid ? periods.GetValueOrDefault(pid) : null,
            MmcAmount = x.MmcAmount,
            MmcPeriodFrom = x.MmcPeriodFrom,
            MmcPeriodTo = x.MmcPeriodTo,
            CreatedAt = x.CreatedAt,
            CreatedByName = users.GetValueOrDefault(x.CreatedBy),
            IsActive = x.IsActive
        }).ToList();
    }

    private static MmcPeriodDto MapPeriod(MmcPeriod x) => new()
    {
        Id = x.IdMmcPeriod,
        PeriodCode = x.PeriodCode,
        PeriodName = x.PeriodName,
        StartDate = x.StartDate,
        EndDate = x.EndDate,
        IsCurrent = x.IsCurrent,
        IsActive = x.IsActive
    };

    private async Task<MmcPeriod> RequirePeriodAsync(int apartmentId, int id, CancellationToken cancellationToken)
    {
        return await db.MmcPeriods.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdMmcPeriod == id && x.IsActive, cancellationToken)
            .ConfigureAwait(false) ?? throw new InvalidOperationException("MMC period not found.");
    }

    private async Task<MmcGridDto> BuildGridAsync(
        int apartmentId,
        MmcPeriod period,
        string? search,
        int? blockId,
        int? unitTypeId,
        Dictionary<int, decimal?>? pendingByUnit,
        Dictionary<int, decimal?>? overrideNewByUnit,
        CancellationToken cancellationToken)
    {
        var q = db.Units.AsNoTracking().Where(u => u.ApartmentId == apartmentId && u.IsActive);
        if (blockId is { } b) q = q.Where(u => u.BlockId == b);
        if (unitTypeId is { } ut) q = q.Where(u => u.UnitTypeId == ut);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(u => u.UnitNumber.Contains(s) || (u.Block != null && u.Block.Contains(s)));
        }
        var units = await q.OrderBy(u => u.UnitNumber).ToListAsync(cancellationToken).ConfigureAwait(false);
        var unitIds = units.Select(x => x.IdUnit).ToList();
        var periodRows = await db.UnitMmcDetails.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.IdMmcPeriod == period.IdMmcPeriod && unitIds.Contains(x.UnitId))
            .ToDictionaryAsync(x => x.UnitId, x => x, cancellationToken).ConfigureAwait(false);
        var ownerByUnit = await LoadPrimaryOwnersByUnitAsync(apartmentId, unitIds, cancellationToken).ConfigureAwait(false);
        if (pendingByUnit is null)
            pendingByUnit = await LoadPendingNewMmcByUnitAsync(apartmentId, period.IdMmcPeriod, cancellationToken).ConfigureAwait(false);

        var lines = units.Select(u =>
        {
            var hasPeriod = periodRows.TryGetValue(u.IdUnit, out var pr);
            var current = hasPeriod ? pr!.MmcAmount : u.CurrentMmcAmount;
            decimal? newVal = overrideNewByUnit?.GetValueOrDefault(u.IdUnit);
            if (newVal is null && pendingByUnit is not null) newVal = pendingByUnit.GetValueOrDefault(u.IdUnit);
            return new MmcGridLineDto
            {
                UnitId = u.IdUnit,
                Block = u.Block,
                UnitNumber = u.UnitNumber,
                PrimaryOwnerName = ownerByUnit.GetValueOrDefault(u.IdUnit),
                CurrentMmcAmount = current,
                NewMmcAmount = newVal,
                HasPeriodRow = hasPeriod
            };
        }).ToList();

        return new MmcGridDto
        {
            MmcPeriodId = period.IdMmcPeriod,
            PeriodName = period.PeriodName,
            Lines = lines,
            Totals = new MmcGridTotalsDto
            {
                CurrentMonthlyTotal = lines.Sum(x => x.CurrentMmcAmount),
                NewMonthlyTotal = lines.Sum(x => x.NewMmcAmount ?? x.CurrentMmcAmount),
                DeltaTotal = lines.Sum(x => (x.NewMmcAmount ?? x.CurrentMmcAmount) - x.CurrentMmcAmount)
            }
        };
    }

    private async Task<Dictionary<int, string?>> LoadPrimaryOwnersByUnitAsync(
        int apartmentId, IReadOnlyList<int> unitIds, CancellationToken cancellationToken)
    {
        if (unitIds.Count == 0) return [];
        var rows = await (from uo in db.UnitOwners.AsNoTracking()
                where uo.ApartmentId == apartmentId && uo.IsPrimaryOwner && uo.IsActive && unitIds.Contains(uo.UnitId)
                join p in db.Persons.AsNoTracking() on uo.PersonId equals p.IdPerson
                select new { uo.UnitId, p.FullName })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.GroupBy(x => x.UnitId).ToDictionary(g => g.Key, g => (string?)g.First().FullName);
    }

    private async Task<Dictionary<int, decimal?>> LoadPendingNewMmcByUnitAsync(
        int apartmentId, int mmcPeriodId, CancellationToken cancellationToken)
    {
        var batchId = await db.MmcBatches.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.MmcPeriodId == mmcPeriodId && x.StatusCode == BatchPending && x.IsActive)
            .OrderByDescending(x => x.SubmittedAt).Select(x => (int?)x.IdMmcBatch)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (batchId is null) return [];
        return await db.MmcBatchLines.AsNoTracking()
            .Where(x => x.MmcBatchId == batchId.Value)
            .ToDictionaryAsync(x => x.UnitId, x => (decimal?)x.NewMmcAmount, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Dictionary<int, decimal>> ResolveCurrentMmcByUnitAsync(
        int apartmentId, int mmcPeriodId, IReadOnlyList<int> unitIds, CancellationToken cancellationToken)
    {
        var units = await db.Units.AsNoTracking().Where(x => x.ApartmentId == apartmentId && unitIds.Contains(x.IdUnit))
            .ToDictionaryAsync(x => x.IdUnit, x => x.CurrentMmcAmount, cancellationToken).ConfigureAwait(false);
        var periodRows = await db.UnitMmcDetails.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.IdMmcPeriod == mmcPeriodId && unitIds.Contains(x.UnitId))
            .ToDictionaryAsync(x => x.UnitId, x => x.MmcAmount, cancellationToken).ConfigureAwait(false);
        var map = new Dictionary<int, decimal>();
        foreach (var id in unitIds)
            map[id] = periodRows.TryGetValue(id, out var p) ? p : units.GetValueOrDefault(id);
        return map;
    }

    private async Task<int> ResolveIncomeHeadIdAsync(int apartmentId, int unitId, CancellationToken cancellationToken)
    {
        var existing = await db.UnitMmcDetails.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.UnitId == unitId)
            .OrderByDescending(x => x.MmcPeriodFrom)
            .Select(x => (int?)x.IncomeHeadId)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (existing is { } e) return e;
        var fallback = await db.IncomeHeads.AsNoTracking()
            .Where(x => x.IsActive && (x.ApartmentId == apartmentId || x.ApartmentId == null))
            .OrderByDescending(x => x.ApartmentId.HasValue).ThenBy(x => x.SortOrder)
            .Select(x => (int?)x.IdIncomeHead)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (fallback is not { } f) throw new InvalidOperationException("No income head configured for MMC.");
        return f;
    }
}
