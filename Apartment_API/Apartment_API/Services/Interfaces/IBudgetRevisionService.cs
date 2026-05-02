using Apartment_API.DTO;
using Microsoft.AspNetCore.Http;

namespace Apartment_API.Services.Interfaces;

public interface IBudgetRevisionService
{
    Task<PagedResult<BudgetRevisionBatchListItemDto>> ListBatchesAsync(
        int apartmentId,
        int? fiscalYearId,
        string? status,
        int? budgetId,
        int? createdBy,
        DateOnly? from,
        DateOnly? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<BudgetRevisionBatchDetailDto?> GetBatchAsync(int apartmentId, string batchId, CancellationToken cancellationToken = default);

    Task<BudgetRevisionLineDetailDto?> GetLineAsync(int apartmentId, int idBudgetRevision, CancellationToken cancellationToken = default);

    Task<CreateBudgetRevisionBatchResultDto> CreateDraftAsync(
        int apartmentId, int userId, CreateBudgetRevisionBatchRequest request, CancellationToken cancellationToken = default);

    Task<CreateBudgetRevisionBatchResultDto> ReplaceDraftBatchAsync(
        int apartmentId, int userId, string batchId, UpdateBudgetRevisionBatchRequest request, CancellationToken cancellationToken = default);

    Task<BudgetRevisionLineDetailDto> PatchLineAsync(
        int apartmentId, int userId, int idBudgetRevision, PatchBudgetRevisionLineRequest request, CancellationToken cancellationToken = default);

    Task DeleteLineAsync(int apartmentId, int userId, int idBudgetRevision, CancellationToken cancellationToken = default);

    Task DeleteBatchAsync(int apartmentId, int userId, string batchId, CancellationToken cancellationToken = default);

    Task<BudgetRevisionSubmitResultDto> SubmitAsync(
        int apartmentId, int userId, string batchId, BudgetRevisionSubmitRequest request, CancellationToken cancellationToken = default);

    Task<BudgetRevisionBatchDetailDto> RecallAsync(
        int apartmentId, int userId, string batchId, BudgetRevisionRecallRequest request, CancellationToken cancellationToken = default);

    Task<BudgetRevisionApproveResultDto> ApproveAsync(
        int apartmentId, int userId, string batchId, BudgetRevisionApproveRequest request, CancellationToken cancellationToken = default);

    Task<BudgetRevisionRejectResultDto> RejectAsync(
        int apartmentId, int userId, string batchId, BudgetRevisionRejectRequest request, CancellationToken cancellationToken = default);

    Task<BudgetRevisionFileUploadDto> UploadFileAsync(int apartmentId, int userId, IFormFile file, CancellationToken cancellationToken = default);

    Task<EligibleBudgetListDto> GetEligibleBudgetsAsync(int apartmentId, int fiscalYearId, CancellationToken cancellationToken = default);

    Task<BudgetRevisionSummaryDto> GetSummaryAsync(int apartmentId, int fiscalYearId, CancellationToken cancellationToken = default);
}
