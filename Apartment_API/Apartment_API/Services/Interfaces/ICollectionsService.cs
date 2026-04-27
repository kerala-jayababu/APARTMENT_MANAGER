using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface ICollectionsService
{
    Task<CollectionsSummaryDto> GetSummaryAsync(int apartmentId, string? period, CancellationToken cancellationToken = default);
    Task<PagedResult<CollectionInvoiceListItemDto>> ListInvoicesAsync(
        int apartmentId, string? search, string? period, int? incomeHeadId, int? statusId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<CollectionInvoiceDetailDto?> GetInvoiceAsync(int apartmentId, int invoiceId, CancellationToken cancellationToken = default);
    Task<(byte[] Content, string ContentType, string FileName)> ExportInvoicesAsync(
        int apartmentId, string format, string? search, string? period, int? incomeHeadId, int? statusId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuickPostPendingItemDto>> GetQuickPostPendingAsync(
        int apartmentId, string? search, int? unitId, DateTime? fromDate, int? incomeHeadId, CancellationToken cancellationToken = default);
    Task<SaveQuickPostReceiptsResponseDto> SaveQuickPostReceiptsAsync(
        int apartmentId, int userId, SaveQuickPostReceiptsRequest request, CancellationToken cancellationToken = default);
    Task<ReceiptDetailDto?> GetReceiptAsync(int apartmentId, long receiptId, CancellationToken cancellationToken = default);
    Task<CancelReceiptResponseDto> CancelReceiptAsync(
        int apartmentId, int userId, long receiptId, CancelReceiptRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MasterLookupItemDto>> GetPaymentModesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MasterLookupItemDto>> GetIncomeHeadsAsync(int apartmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MasterLookupItemDto>> GetInvoiceStatusesAsync(CancellationToken cancellationToken = default);
}
