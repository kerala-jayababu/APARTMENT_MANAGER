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
        if (dto.IdExpenseHead <= 0)
        {
            var e = new ExpenseHead
            {
                ApartmentId = apartmentId,
                HeadCode = dto.HeadCode.Trim(),
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

        existing.HeadCode = dto.HeadCode.Trim();
        existing.HeadName = dto.HeadName.Trim();
        existing.SortOrder = dto.SortOrder;
        existing.IsActive = dto.IsActive;
        existing.ApartmentId = apartmentId;
        existing.LedgerAccountId = dto.LedgerAccountId;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new EntitySaveResult<ExpenseHeadDto> { Data = existing.ToDto(), Created = false };
    }
}
