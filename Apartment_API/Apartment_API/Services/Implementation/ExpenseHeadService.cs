using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class ExpenseHeadService(AppDbContext db) : IExpenseHeadService
{
    public async Task<IReadOnlyList<ExpenseHeadDto>> ListExpenseHeadsForApartmentAsync(
        int apartmentId, CancellationToken cancellationToken = default)
    {
        var list = await db.ExpenseHeads.AsNoTracking()
            .Where(x => !x.ApartmentId.HasValue || x.ApartmentId == apartmentId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.HeadName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<ExpenseHeadDto?> GetByIdAsync(
        int idExpenseHead, int apartmentId, CancellationToken cancellationToken = default)
    {
        var e = await db.ExpenseHeads.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.IdExpenseHead == idExpenseHead
                     && (!x.ApartmentId.HasValue || x.ApartmentId == apartmentId),
                cancellationToken)
            .ConfigureAwait(false);
        return e?.ToDto();
    }

    public async Task<EntitySaveResult<ExpenseHeadDto>?> SaveAsync(
        ExpenseHeadSaveDto dto,
        int createdByUserId,
        int apartmentId,
        CancellationToken cancellationToken = default)
    {
        var headCode = dto.HeadCode.Trim();
        if (string.IsNullOrEmpty(headCode))
            throw new InvalidOperationException("HeadCode is required.");

        var excludeId = dto.IdExpenseHead > 0 ? dto.IdExpenseHead : (int?)null;
        await EnsureHeadCodeUniqueAsync(apartmentId, headCode, excludeId, cancellationToken).ConfigureAwait(false);
        await EnsureSortOrderUniqueAsync(apartmentId, dto.SortOrder, excludeId, cancellationToken).ConfigureAwait(false);

        if (dto.IdExpenseHead <= 0)
        {
            var e = new ExpenseHead
            {
                ApartmentId = apartmentId,
                HeadCode = headCode,
                HeadName = dto.HeadName.Trim(),
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdByUserId,
                LedgerAccountId = dto.LedgerAccountId
            };
            db.ExpenseHeads.Add(e);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new EntitySaveResult<ExpenseHeadDto> { Data = e.ToDto(), Created = true };
        }

        var existing = await db.ExpenseHeads
            .FirstOrDefaultAsync(
                x => x.IdExpenseHead == dto.IdExpenseHead
                     && (!x.ApartmentId.HasValue || x.ApartmentId == apartmentId),
                cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            return null;

        existing.HeadCode = headCode;
        existing.HeadName = dto.HeadName.Trim();
        existing.SortOrder = dto.SortOrder;
        existing.IsActive = dto.IsActive;
        existing.ApartmentId = apartmentId;
        existing.LedgerAccountId = dto.LedgerAccountId;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new EntitySaveResult<ExpenseHeadDto> { Data = existing.ToDto(), Created = false };
    }

    private static IQueryable<ExpenseHead> HeadsForApartment(IQueryable<ExpenseHead> q, int apartmentId) =>
        q.Where(x => x.IsActive && (!x.ApartmentId.HasValue || x.ApartmentId == apartmentId));

    private async Task EnsureHeadCodeUniqueAsync(
        int apartmentId, string headCode, int? excludeIdExpenseHead, CancellationToken cancellationToken)
    {
        var normalized = headCode.ToUpperInvariant();
        var q = HeadsForApartment(db.ExpenseHeads.AsNoTracking(), apartmentId);
        if (excludeIdExpenseHead is { } exclude)
            q = q.Where(x => x.IdExpenseHead != exclude);

        var taken = await q.AnyAsync(x => x.HeadCode.ToUpper() == normalized, cancellationToken).ConfigureAwait(false);
        if (taken)
            throw new InvalidOperationException(
                $"An expense head with head code '{headCode}' already exists for this apartment.");
    }

    private async Task EnsureSortOrderUniqueAsync(
        int apartmentId, byte sortOrder, int? excludeIdExpenseHead, CancellationToken cancellationToken)
    {
        var q = HeadsForApartment(db.ExpenseHeads.AsNoTracking(), apartmentId)
            .Where(x => x.SortOrder == sortOrder);
        if (excludeIdExpenseHead is { } exclude)
            q = q.Where(x => x.IdExpenseHead != exclude);

        var taken = await q.AnyAsync(cancellationToken).ConfigureAwait(false);
        if (taken)
            throw new InvalidOperationException(
                $"sortOrder {sortOrder} is already used by another active expense head in this apartment.");
    }
}
