using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IIncomeHeadService
{
    Task<IReadOnlyList<IncomeHeadDto>> ListIncomeHeadsForApartmentAsync(int apartmentId, CancellationToken cancellationToken = default);

    /// <summary>Global heads (no apartment) or row for <paramref name="apartmentId" />, same rule as the list.</summary>
    Task<IncomeHeadDto?> GetByIdAsync(int idIncomeHead, int apartmentId, CancellationToken cancellationToken = default);

    /// <summary>Insert when <c>IdIncomeHead &lt;= 0</c>. Update scoped to <paramref name="apartmentId" /> from the access token.</summary>
    Task<EntitySaveResult<IncomeHeadDto>?> SaveAsync(
        IncomeHeadSaveDto dto,
        int createdByUserId,
        int apartmentId,
        CancellationToken cancellationToken = default);
}
