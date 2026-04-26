using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation.Committee;

public sealed class CommitteeTenureService(AppDbContext db, CommitteeDataHelper helper) : ICommitteeTenureService
{
    public async Task<PagedResult<CommitteeTenureListDto>> ListTenuresAsync(
        int apartmentId, bool? includeArchived, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = db.CommitteeTenures.AsNoTracking().Where(t => t.ApartmentId == apartmentId);
        if (includeArchived == false) q = q.Where(t => t.IsActive);
        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderByDescending(t => t.IsActive)
            .ThenByDescending(t => t.TenureStartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return new PagedResult<CommitteeTenureListDto> { Items = [], TotalCount = total, Page = page, PageSize = pageSize };
        var today = DateTime.UtcNow.Date;
        var activeStatusId = await helper.GetStatusIdByCodeAsync(Committee.StatusActive, cancellationToken);
        var tenureIds = rows.Select(t => t.IdCommitteeTenure).ToList();
        var (counts, keyNames) = await helper.LoadMemberCountsAndKeyNamesAsync(
            apartmentId, tenureIds, activeStatusId, today, cancellationToken);
        var items = rows.Select(t => new CommitteeTenureListDto
        {
            Id = t.IdCommitteeTenure,
            TenureName = Committee.FormatTenureName(t),
            TenureStartDate = t.TenureStartDate,
            TenureEndDate = t.TenureEndDate,
            MemberCount = counts.GetValueOrDefault(t.IdCommitteeTenure, 0),
            PresidentName = keyNames.GetValueOrDefault(t.IdCommitteeTenure).President,
            SecretaryName = keyNames.GetValueOrDefault(t.IdCommitteeTenure).Secretary,
            TreasurerName = keyNames.GetValueOrDefault(t.IdCommitteeTenure).Treasurer,
            IsActive = t.IsActive,
            DaysRemaining = Committee.DaysRemaining(t, today)
        }).ToList();
        return new PagedResult<CommitteeTenureListDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<CommitteeTenureDetailDto?> GetActiveTenureAsync(int apartmentId, CancellationToken cancellationToken = default)
    {
        var t = await db.CommitteeTenures.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IsActive, cancellationToken);
        if (t is null) return null;
        return await MapToTenureDetailAsync(apartmentId, t, cancellationToken);
    }

    public async Task<CommitteeTenureDetailDto?> GetTenureByIdAsync(
        int apartmentId, int id, CancellationToken cancellationToken = default)
    {
        var t = await db.CommitteeTenures.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdCommitteeTenure == id, cancellationToken);
        if (t is null) return null;
        return await MapToTenureDetailAsync(apartmentId, t, cancellationToken);
    }

    private async Task<CommitteeTenureDetailDto> MapToTenureDetailAsync(
        int apartmentId, CommitteeTenure t, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var members = await helper.GetMemberDetailItemsForTenureAsync(apartmentId, t.IdCommitteeTenure, cancellationToken);
        return new CommitteeTenureDetailDto
        {
            Id = t.IdCommitteeTenure,
            TenureName = Committee.FormatTenureName(t),
            TenureStartDate = t.TenureStartDate,
            TenureEndDate = t.TenureEndDate,
            Notes = t.Notes,
            DaysRemaining = Committee.DaysRemaining(t, today),
            IsActive = t.IsActive,
            Members = members
        };
    }

    public async Task<int> CreateTenureAsync(
        int apartmentId, int userId, CreateCommitteeTenureRequest request, CancellationToken cancellationToken = default)
    {
        if (request.TenureEndDate <= request.TenureStartDate) throw new InvalidOperationException("TenureEndDate must be after TenureStartDate.");
        var days = (request.TenureEndDate.Date - request.TenureStartDate.Date).TotalDays;
        if (days is < 350 or > 1125) throw new InvalidOperationException("Typical MC tenure is 1–3 years (350–1125 days).");

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        var actives = await db.CommitteeTenures
            .Where(t => t.ApartmentId == apartmentId && t.IsActive)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var a in actives)
        {
            a.IsActive = false;
            a.UpdatedAt = now;
            a.UpdatedBy = userId;
        }
        var t = new CommitteeTenure
        {
            ApartmentId = apartmentId,
            TenureStartDate = request.TenureStartDate.Date,
            TenureEndDate = request.TenureEndDate.Date,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes!.Trim(),
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId
        };
        db.CommitteeTenures.Add(t);
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return t.IdCommitteeTenure;
    }

    public async Task UpdateTenureAsync(
        int apartmentId, int userId, int id, CreateCommitteeTenureRequest request, CancellationToken cancellationToken = default)
    {
        if (request.TenureEndDate <= request.TenureStartDate) throw new InvalidOperationException("TenureEndDate must be after TenureStartDate.");
        var t = await db.CommitteeTenures
            .FirstOrDefaultAsync(x => x.IdCommitteeTenure == id && x.ApartmentId == apartmentId, cancellationToken);
        if (t is null) throw new InvalidOperationException("Tenure not found.");
        if (!t.IsActive) throw new InvalidOperationException("Only the active MC tenure can be edited.");
        var now = DateTime.UtcNow;
        t.TenureStartDate = request.TenureStartDate.Date;
        t.TenureEndDate = request.TenureEndDate.Date;
        t.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes!.Trim();
        t.UpdatedAt = now;
        t.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ExtendTenureAsync(
        int apartmentId, int userId, int id, ExtendTenureRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ExtensionReason) || request.ExtensionReason.Length > 500)
            throw new InvalidOperationException("ExtensionReason is required and must be at most 500 characters.");
        var t = await db.CommitteeTenures
            .FirstOrDefaultAsync(x => x.IdCommitteeTenure == id && x.ApartmentId == apartmentId, cancellationToken);
        if (t is null) throw new InvalidOperationException("Tenure not found.");
        if (!t.IsActive) throw new InvalidOperationException("Only the active MC tenure can be extended.");
        if (request.NewEndDate <= t.TenureEndDate) throw new InvalidOperationException("newEndDate must be after the current end date.");
        var prev = t.TenureEndDate;
        var now = DateTime.UtcNow;
        t.TenureEndDate = request.NewEndDate.Date;
        t.UpdatedAt = now;
        t.UpdatedBy = userId;
        db.CommitteeTenureExtensionLogs.Add(new CommitteeTenureExtensionLog
        {
            ApartmentId = apartmentId,
            CommitteeTenureId = id,
            PreviousEndDate = prev.Date,
            NewEndDate = request.NewEndDate.Date,
            ExtensionReason = request.ExtensionReason.Trim(),
            ExtendedByUserId = userId,
            ExtendedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<CommitteeTenureHistoryDto>> GetTenureHistoryPageAsync(
        int apartmentId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = db.CommitteeTenures.AsNoTracking().Where(t => t.ApartmentId == apartmentId);
        if (from is { } f) q = q.Where(x => x.TenureEndDate >= f.Date);
        if (to is { } toD) q = q.Where(x => x.TenureStartDate <= toD.Date);
        var total = await q.CountAsync(cancellationToken);
        var rows = await q.OrderByDescending(t => t.TenureStartDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return new PagedResult<CommitteeTenureHistoryDto> { Items = [], TotalCount = total, Page = page, PageSize = pageSize };
        var today = DateTime.UtcNow.Date;
        var activeStatusId = await helper.GetStatusIdByCodeAsync(Committee.StatusActive, cancellationToken);
        var tenureIds = rows.Select(t => t.IdCommitteeTenure).ToList();
        var (counts, keyNames) = await helper.LoadMemberCountsAndKeyNamesAsync(
            apartmentId, tenureIds, activeStatusId, today, cancellationToken);
        var items = rows.Select(t => new CommitteeTenureHistoryDto
        {
            Id = t.IdCommitteeTenure,
            TenureName = Committee.FormatTenureName(t),
            TenureStartDate = t.TenureStartDate,
            TenureEndDate = t.TenureEndDate,
            PresidentName = keyNames.GetValueOrDefault(t.IdCommitteeTenure).President,
            SecretaryName = keyNames.GetValueOrDefault(t.IdCommitteeTenure).Secretary,
            TreasurerName = keyNames.GetValueOrDefault(t.IdCommitteeTenure).Treasurer,
            MemberCount = counts.GetValueOrDefault(t.IdCommitteeTenure, 0),
            IsActive = t.IsActive
        }).ToList();
        return new PagedResult<CommitteeTenureHistoryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<IReadOnlyList<TenureExtensionDto>> GetExtensionsForTenureAsync(
        int apartmentId, int tenureId, CancellationToken cancellationToken = default)
    {
        var exists = await db.CommitteeTenures.AsNoTracking()
            .AnyAsync(t => t.IdCommitteeTenure == tenureId && t.ApartmentId == apartmentId, cancellationToken);
        if (!exists) return [];
        return await (from e in db.CommitteeTenureExtensionLogs.AsNoTracking()
            where e.ApartmentId == apartmentId && e.CommitteeTenureId == tenureId
            join u in db.Users.AsNoTracking() on e.ExtendedByUserId equals u.IdUser into uj
            from u in uj.DefaultIfEmpty()
            orderby e.ExtendedAt descending
            select new TenureExtensionDto
            {
                Id = e.IdCommitteeTenureExtensionLog,
                PreviousEndDate = e.PreviousEndDate,
                NewEndDate = e.NewEndDate,
                ExtensionReason = e.ExtensionReason,
                ExtendedByUserId = e.ExtendedByUserId,
                ExtendedByName = u != null ? u.FullName : null,
                ExtendedAt = e.ExtendedAt
            }).ToListAsync(cancellationToken);
    }
}
