using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IBankAccountService
{
    Task<IReadOnlyList<BankAccountDto>> ListBankAccountsForApartmentAsync(int apartmentId, CancellationToken cancellationToken = default);

    /// <summary>Resolves by id when the account belongs to <paramref name="apartmentId" />.</summary>
    Task<BankAccountDto?> GetByIdAsync(int idBankAccount, int apartmentId, CancellationToken cancellationToken = default);

    /// <summary>Insert when <c>IdBankAccount &lt;= 0</c>; else update. Scoped to <paramref name="apartmentId" /> (from the access token).</summary>
    Task<EntitySaveResult<BankAccountDto>?> SaveAsync(
        BankAccountSaveDto dto,
        int userId,
        int apartmentId,
        CancellationToken cancellationToken = default);
}
