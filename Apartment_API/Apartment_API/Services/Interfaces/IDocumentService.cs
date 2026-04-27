using Apartment_API.DTO;
using Microsoft.AspNetCore.Http;

namespace Apartment_API.Services.Interfaces;

public interface IDocumentService
{
    Task<PagedResult<DocumentListDto>> ListAsync(
        int apartmentId,
        string? search,
        int? categoryId,
        string? linkedEntityType,
        int? linkedEntityId,
        int? uploadedByUserId,
        DateTime? uploadedFrom,
        DateTime? uploadedTo,
        int? expiringWithinDays,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDir,
        CancellationToken cancellationToken = default);

    Task<DocumentDetailDto?> GetAsync(int apartmentId, int id, CancellationToken cancellationToken = default);
    Task<int> UploadAsync(
        int apartmentId,
        int userId,
        UploadDocumentRequest request,
        IFormFile file,
        CancellationToken cancellationToken = default);
    Task UpdateAsync(
        int apartmentId,
        int userId,
        int id,
        UpdateDocumentRequest request,
        CancellationToken cancellationToken = default);
    Task<(Stream Stream, string ContentType, string FileName, long? Length)> DownloadAsync(
        int apartmentId,
        int id,
        CancellationToken cancellationToken = default);
    Task<DocumentPreviewUrlDto> GetPreviewUrlAsync(int apartmentId, int id, CancellationToken cancellationToken = default);
    Task DeleteAsync(int apartmentId, int userId, int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentCategoryDto>> ListCategoriesAsync(bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LinkedEntityDto>> SearchLinkedEntitiesAsync(
        int apartmentId,
        string type,
        string? search,
        int take,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpiringDocumentDto>> GetExpiringAsync(
        int apartmentId,
        int windowDays,
        int? categoryId,
        CancellationToken cancellationToken = default);
    Task<DocumentStatsDto> GetStatsAsync(int apartmentId, CancellationToken cancellationToken = default);
}
