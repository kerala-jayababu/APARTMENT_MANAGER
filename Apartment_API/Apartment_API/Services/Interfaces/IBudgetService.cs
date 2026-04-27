using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IBudgetService
{
    Task<BudgetDetailDto> GetBudgetDetailAsync(int apartmentId, int fiscalYearId, CancellationToken cancellationToken = default);
    Task<BudgetDetailDto> SaveBudgetLinesAsync(
        int apartmentId, int userId, int fiscalYearId, SaveBudgetRequest request, CancellationToken cancellationToken = default);
}
