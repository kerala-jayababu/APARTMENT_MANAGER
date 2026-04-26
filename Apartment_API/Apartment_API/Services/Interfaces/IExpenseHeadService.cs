using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IExpenseHeadService
{
    Task<IReadOnlyList<ExpenseHeadDto>> ListExpenseHeadsForApartmentAsync(int apartmentId, CancellationToken cancellationToken = default);

    /// <summary>Global heads (no apartment) or row for <paramref name="apartmentId" />, same rule as the list.</summary>
    Task<ExpenseHeadDto?> GetByIdAsync(int idExpenseHead, int apartmentId, CancellationToken cancellationToken = default);

    /// <summary>Insert when <c>IdExpenseHead &lt;= 0</c>. Update scoped to <paramref name="apartmentId" /> from the access token.</summary>
    Task<EntitySaveResult<ExpenseHeadDto>?> SaveAsync(
        ExpenseHeadSaveDto dto,
        int createdByUserId,
        int apartmentId,
        CancellationToken cancellationToken = default);
}
