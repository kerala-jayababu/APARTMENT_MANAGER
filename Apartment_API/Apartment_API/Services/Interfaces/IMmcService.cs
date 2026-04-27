using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IMmcService
{
    Task<IReadOnlyList<MmcPeriodDto>> ListPeriodsAsync(int apartmentId, bool? isActive, CancellationToken cancellationToken = default);
    Task<MmcPeriodDto?> GetCurrentPeriodAsync(int apartmentId, CancellationToken cancellationToken = default);
    Task<int> CreatePeriodAsync(int apartmentId, int userId, CreateMmcPeriodRequest request, CancellationToken cancellationToken = default);

    Task<MmcGridDto> GetGridAsync(
        int apartmentId, int mmcPeriodId, string? search, int? blockId, int? unitTypeId, CancellationToken cancellationToken = default);
    Task<MmcBatchCreatedDto> SubmitBatchAsync(int apartmentId, int userId, SubmitMmcBatchRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<MmcBatchListDto>> ListBatchesAsync(
        int apartmentId, int? mmcPeriodId, string? statusCode, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<MmcBatchDetailDto?> GetBatchAsync(int apartmentId, int id, CancellationToken cancellationToken = default);
    Task ApproveBatchAsync(int apartmentId, int userId, int id, CancellationToken cancellationToken = default);
    Task RejectBatchAsync(int apartmentId, int userId, int id, RejectRequest request, CancellationToken cancellationToken = default);
    Task<MmcGridDto> CopyFromPeriodAsync(
        int apartmentId, int mmcPeriodId, int sourcePeriodId, string? search, int? blockId, int? unitTypeId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnitMmcHistoryDto>> GetUnitHistoryAsync(int apartmentId, int unitId, CancellationToken cancellationToken = default);
}
