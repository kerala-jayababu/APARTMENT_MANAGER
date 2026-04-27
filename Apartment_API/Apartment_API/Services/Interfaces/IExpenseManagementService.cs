using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IExpenseManagementService
{
    Task<ExpenseSummaryDto> GetSummaryAsync(int apartmentId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<PagedResult<ExpenseBillListItemDto>> ListBillsAsync(
        int apartmentId, string? search, int? expenseHeadId, string? statusCode, int? vendorId, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<object?> GetBillAsync(int apartmentId, long billId, CancellationToken cancellationToken = default);
    Task<ExpenseBillUpsertResponseDto> CreateRegularAsync(int apartmentId, int userId, CreateExpenseBillRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseBillUpsertResponseDto> CreateContractAsync(int apartmentId, int userId, CreateContractExpenseRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseBillUpsertResponseDto> UpdateDraftAsync(int apartmentId, int userId, long billId, CreateExpenseBillRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseBillDeleteResponseDto> DeleteDraftAsync(int apartmentId, long billId, CancellationToken cancellationToken = default);
    Task<(byte[] Content, string ContentType, string FileName)> DownloadAttachmentAsync(int apartmentId, long billId, CancellationToken cancellationToken = default);
    Task<ExpenseCalculateResponseDto> CalculateAsync(ExpenseCalculateRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseBudgetCheckDto> BudgetCheckAsync(int apartmentId, int expenseHeadId, int fiscalYearId, decimal? additionalAmount, CancellationToken cancellationToken = default);
    Task<(byte[] Content, string ContentType, string FileName)> ExportBillsAsync(
        int apartmentId, string format, string? search, int? expenseHeadId, string? statusCode, int? vendorId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseSimpleLookupDto>> GetVendorsAsync(int apartmentId, bool isActive, string? search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseSimpleLookupDto>> GetExpenseHeadsAsync(int apartmentId, bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseSimpleLookupDto>> GetBankAccountsAsync(int apartmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseSimpleLookupDto>> GetFiscalYearsAsync(int apartmentId, CancellationToken cancellationToken = default);
}
