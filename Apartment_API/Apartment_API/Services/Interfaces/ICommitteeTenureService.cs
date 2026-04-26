using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface ICommitteeTenureService
{
    Task<PagedResult<CommitteeTenureListDto>> ListTenuresAsync(
        int apartmentId, bool? includeArchived, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<CommitteeTenureDetailDto?> GetActiveTenureAsync(int apartmentId, CancellationToken cancellationToken = default);
    Task<CommitteeTenureDetailDto?> GetTenureByIdAsync(int apartmentId, int id, CancellationToken cancellationToken = default);
    Task<int> CreateTenureAsync(int apartmentId, int userId, CreateCommitteeTenureRequest request, CancellationToken cancellationToken = default);
    Task UpdateTenureAsync(int apartmentId, int userId, int id, CreateCommitteeTenureRequest request, CancellationToken cancellationToken = default);
    Task ExtendTenureAsync(int apartmentId, int userId, int id, ExtendTenureRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<CommitteeTenureHistoryDto>> GetTenureHistoryPageAsync(
        int apartmentId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenureExtensionDto>> GetExtensionsForTenureAsync(
        int apartmentId, int tenureId, CancellationToken cancellationToken = default);
}
