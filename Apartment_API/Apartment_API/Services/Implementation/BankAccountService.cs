using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class BankAccountService(AppDbContext db) : IBankAccountService
{
    public async Task<IReadOnlyList<BankAccountDto>> ListBankAccountsForApartmentAsync(
        int apartmentId, CancellationToken cancellationToken = default)
    {
        var list = await db.BankAccounts.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId)
            .OrderBy(x => x.AccountName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<BankAccountDto?> GetByIdAsync(
        int idBankAccount, int apartmentId, CancellationToken cancellationToken = default)
    {
        var e = await db.BankAccounts.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.IdBankAccount == idBankAccount && x.ApartmentId == apartmentId,
                cancellationToken)
            .ConfigureAwait(false);
        return e?.ToDto();
    }

    public async Task<EntitySaveResult<BankAccountDto>?> SaveAsync(
        BankAccountSaveDto dto, int userId, int apartmentId, CancellationToken cancellationToken = default)
    {
        if (dto.IdBankAccount <= 0)
        {
            var e = new BankAccount
            {
                ApartmentId = apartmentId,
                AccountName = dto.AccountName.Trim(),
                BankName = dto.BankName.Trim(),
                BankAccountTypeId = dto.BankAccountTypeId,
                AccountNumber = dto.AccountNumber.Trim(),
                IfscCode = dto.IfscCode.Trim(),
                BranchName = string.IsNullOrWhiteSpace(dto.BranchName) ? null : dto.BranchName.Trim(),
                AccountHolderName = dto.AccountHolderName.Trim(),
                OpeningBalance = dto.OpeningBalance,
                CurrentBalance = dto.CurrentBalance,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                LedgerAccountId = dto.LedgerAccountId,
                FundId = dto.FundId
            };
            db.BankAccounts.Add(e);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new EntitySaveResult<BankAccountDto> { Data = e.ToDto(), Created = true };
        }

        var existing = await db.BankAccounts
            .FirstOrDefaultAsync(
                x => x.IdBankAccount == dto.IdBankAccount && x.ApartmentId == apartmentId,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            return null;

        existing.AccountName = dto.AccountName.Trim();
        existing.BankName = dto.BankName.Trim();
        existing.BankAccountTypeId = dto.BankAccountTypeId;
        existing.AccountNumber = dto.AccountNumber.Trim();
        existing.IfscCode = dto.IfscCode.Trim();
        existing.BranchName = string.IsNullOrWhiteSpace(dto.BranchName) ? null : dto.BranchName.Trim();
        existing.AccountHolderName = dto.AccountHolderName.Trim();
        existing.OpeningBalance = dto.OpeningBalance;
        existing.CurrentBalance = dto.CurrentBalance;
        existing.IsActive = dto.IsActive;
        existing.LedgerAccountId = dto.LedgerAccountId;
        existing.FundId = dto.FundId;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new EntitySaveResult<BankAccountDto> { Data = existing.ToDto(), Created = false };
    }
}
