using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IBlockService
{
    Task<IReadOnlyList<BlockListDto>> ListAsync(
        int apartmentId, string? search, bool? isActive, CancellationToken cancellationToken = default);
    Task<BlockListDto?> GetByIdAsync(int apartmentId, int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(int apartmentId, int userId, CreateBlockRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(int apartmentId, int userId, int id, CreateBlockRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int apartmentId, int id, CancellationToken cancellationToken = default);
}
