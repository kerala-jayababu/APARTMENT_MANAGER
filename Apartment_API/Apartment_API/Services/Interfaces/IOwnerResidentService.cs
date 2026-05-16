using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IOwnerResidentService
{
    Task<PagedResult<OwnerListDto>> ListAsync(
        int apartmentId, string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<OwnerDetailDto?> GetAsync(int apartmentId, int personId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(int apartmentId, int userId, CreateOwnerRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(int apartmentId, int userId, int personId, CreateOwnerRequest request, CancellationToken cancellationToken = default);
    Task<IdProofResultDto> UploadIdProofAsync(
        int apartmentId, int userId, int personId, Stream fileStream, string fileName, int documentCategoryId,
        CancellationToken cancellationToken = default);
}
