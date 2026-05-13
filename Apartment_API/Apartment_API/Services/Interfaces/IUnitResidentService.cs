using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IUnitResidentService
{
    Task<PagedResult<UnitListDto>> ListUnitsAsync(
        int apartmentId, string? search, int? blockId, int? unitTypeId, int? unitStatusId, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<UnitDetailDto?> GetUnitAsync(int apartmentId, int id, CancellationToken cancellationToken = default);
    Task<int> CreateUnitAsync(int apartmentId, int userId, CreateUnitRequest request, CancellationToken cancellationToken = default);
    Task UpdateUnitAsync(int apartmentId, int userId, int id, CreateUnitRequest request, CancellationToken cancellationToken = default);
    Task<BulkGenerateResultDto> BulkGenerateAsync(int apartmentId, int userId, BulkGenerateUnitsRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlockOccupancyDto>> GetOccupancyAsync(int apartmentId, int? blockId, CancellationToken cancellationToken = default);
    Task<ChangeUnitStatusResultDto> ChangeStatusAsync(int apartmentId, int userId, int unitId, ChangeUnitStatusRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<UnitStatusHistoryDto>> GetStatusHistoryAsync(
        int apartmentId, int? unitId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnitStatusHistoryDto>> GetStatusHistoryForUnitAsync(
        int apartmentId, int unitId, CancellationToken cancellationToken = default);
}
