using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class IncomeHeadService(AppDbContext db) : IIncomeHeadService
{
    public async Task<IReadOnlyList<IncomeHeadDto>> ListIncomeHeadsForApartmentAsync(
        int apartmentId, CancellationToken cancellationToken = default)
    {
        var list = await db.IncomeHeads.AsNoTracking()
            .Where(x => !x.ApartmentId.HasValue || x.ApartmentId == apartmentId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.HeadName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<IncomeHeadDto?> GetByIdAsync(
        int idIncomeHead, int apartmentId, CancellationToken cancellationToken = default)
    {
        var e = await db.IncomeHeads.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.IdIncomeHead == idIncomeHead
                     && (!x.ApartmentId.HasValue || x.ApartmentId == apartmentId),
                cancellationToken)
            .ConfigureAwait(false);
        return e?.ToDto();
    }

    public async Task<EntitySaveResult<IncomeHeadDto>?> SaveAsync(
        IncomeHeadSaveDto dto,
        int createdByUserId,
        int apartmentId,
        CancellationToken cancellationToken = default)
    {
        if (dto.IdIncomeHead <= 0)
        {
            var e = new IncomeHead
            {
                ApartmentId = apartmentId,
                HeadCode = dto.HeadCode.Trim(),
                HeadName = dto.HeadName.Trim(),
                IsAutoInvoiced = dto.IsAutoInvoiced,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdByUserId,
                LedgerAccountId = dto.LedgerAccountId
            };
            db.IncomeHeads.Add(e);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new EntitySaveResult<IncomeHeadDto> { Data = e.ToDto(), Created = true };
        }

        var existing = await db.IncomeHeads
            .FirstOrDefaultAsync(
                x => x.IdIncomeHead == dto.IdIncomeHead
                     && (!x.ApartmentId.HasValue || x.ApartmentId == apartmentId),
                cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            return null;

        existing.HeadCode = dto.HeadCode.Trim();
        existing.HeadName = dto.HeadName.Trim();
        existing.IsAutoInvoiced = dto.IsAutoInvoiced;
        existing.SortOrder = dto.SortOrder;
        existing.IsActive = dto.IsActive;
        existing.ApartmentId = apartmentId;
        existing.LedgerAccountId = dto.LedgerAccountId;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new EntitySaveResult<IncomeHeadDto> { Data = existing.ToDto(), Created = false };
    }
}
