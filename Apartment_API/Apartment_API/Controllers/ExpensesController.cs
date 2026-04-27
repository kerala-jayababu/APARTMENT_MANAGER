using Asp.Versioning;
using Apartment_API.Configuration;
using Apartment_API.DTO;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apartment_API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ApiAccess)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/expenses")]
public sealed class ExpensesController(
    IExpenseManagementService service,
    ICurrentUser currentUser,
    ILogger<ExpensesController> logger) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponseDto<ExpenseSummaryDto>>> Summary(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return Forbidden<ExpenseSummaryDto>();
        try
        {
            var data = await service.GetSummaryAsync(apartmentId, fromDate, toDate, cancellationToken);
            return Ok(new ApiResponseDto<ExpenseSummaryDto> { Success = true, Message = "Expense summary loaded.", Data = data });
        }
        catch (UnauthorizedAccessException) { return ForbidResponse<ExpenseSummaryDto>(); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiResponseDto<ExpenseSummaryDto> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] }); }
        catch (Exception ex) { logger.LogError(ex, "Expense summary."); return ServerError<ExpenseSummaryDto>(); }
    }

    [HttpGet("bills")]
    public async Task<ActionResult<ApiResponseDto<PagedResult<ExpenseBillListItemDto>>>> ListBills(
        [FromQuery] string? search,
        [FromQuery] int? expenseHeadId,
        [FromQuery] string? statusCode,
        [FromQuery] int? vendorId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return Forbidden<PagedResult<ExpenseBillListItemDto>>();
        try
        {
            var data = await service.ListBillsAsync(apartmentId, search, expenseHeadId, statusCode, vendorId, fromDate, toDate, pageNumber, pageSize, cancellationToken);
            return Ok(new ApiResponseDto<PagedResult<ExpenseBillListItemDto>> { Success = true, Message = "Expense bills loaded.", Data = data });
        }
        catch (UnauthorizedAccessException) { return ForbidResponse<PagedResult<ExpenseBillListItemDto>>(); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiResponseDto<PagedResult<ExpenseBillListItemDto>> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] }); }
        catch (Exception ex) { logger.LogError(ex, "Expense bills."); return ServerError<PagedResult<ExpenseBillListItemDto>>(); }
    }

    [HttpGet("bills/{billId:long}")]
    public async Task<ActionResult<ApiResponseDto<object>>> GetBill([FromRoute] long billId, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return Forbidden<object>();
        try
        {
            var data = await service.GetBillAsync(apartmentId, billId, cancellationToken);
            if (data is null) return NotFound(new ApiResponseDto<object> { Success = false, Message = "BILL_NOT_FOUND", Errors = ["BILL_NOT_FOUND"] });
            return Ok(new ApiResponseDto<object> { Success = true, Message = "Expense bill loaded.", Data = data });
        }
        catch (UnauthorizedAccessException) { return ForbidResponse<object>(); }
        catch (Exception ex) { logger.LogError(ex, "Expense bill {BillId}.", billId); return ServerError<object>(); }
    }

    [HttpPost("bills")]
    public async Task<ActionResult<ApiResponseDto<ExpenseBillUpsertResponseDto>>> CreateBill([FromBody] CreateExpenseBillRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId) return Unauthorized(new ApiResponseDto<ExpenseBillUpsertResponseDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId) return Forbidden<ExpenseBillUpsertResponseDto>();
        try
        {
            var data = await service.CreateRegularAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseDto<ExpenseBillUpsertResponseDto> { Success = true, Message = "Expense bill created.", Data = data });
        }
        catch (UnauthorizedAccessException) { return ForbidResponse<ExpenseBillUpsertResponseDto>(); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiResponseDto<ExpenseBillUpsertResponseDto> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] }); }
        catch (Exception ex) { logger.LogError(ex, "Create expense bill."); return ServerError<ExpenseBillUpsertResponseDto>(); }
    }

    [HttpPost("bills/contract")]
    public async Task<ActionResult<ApiResponseDto<ExpenseBillUpsertResponseDto>>> CreateContract([FromBody] CreateContractExpenseRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId) return Unauthorized(new ApiResponseDto<ExpenseBillUpsertResponseDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId) return Forbidden<ExpenseBillUpsertResponseDto>();
        try
        {
            var data = await service.CreateContractAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseDto<ExpenseBillUpsertResponseDto> { Success = true, Message = "Contract expense created.", Data = data });
        }
        catch (UnauthorizedAccessException) { return ForbidResponse<ExpenseBillUpsertResponseDto>(); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiResponseDto<ExpenseBillUpsertResponseDto> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] }); }
        catch (Exception ex) { logger.LogError(ex, "Create contract expense."); return ServerError<ExpenseBillUpsertResponseDto>(); }
    }

    [HttpPut("bills/{billId:long}")]
    public async Task<ActionResult<ApiResponseDto<ExpenseBillUpsertResponseDto>>> UpdateBill([FromRoute] long billId, [FromBody] CreateExpenseBillRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId) return Unauthorized(new ApiResponseDto<ExpenseBillUpsertResponseDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId) return Forbidden<ExpenseBillUpsertResponseDto>();
        try
        {
            var data = await service.UpdateDraftAsync(apartmentId, userId, billId, request, cancellationToken);
            return Ok(new ApiResponseDto<ExpenseBillUpsertResponseDto> { Success = true, Message = "Expense bill updated.", Data = data });
        }
        catch (UnauthorizedAccessException) { return ForbidResponse<ExpenseBillUpsertResponseDto>(); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiResponseDto<ExpenseBillUpsertResponseDto> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] }); }
        catch (Exception ex) { logger.LogError(ex, "Update expense bill {BillId}.", billId); return ServerError<ExpenseBillUpsertResponseDto>(); }
    }

    [HttpDelete("bills/{billId:long}")]
    public async Task<ActionResult<ApiResponseDto<ExpenseBillDeleteResponseDto>>> DeleteBill([FromRoute] long billId, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return Forbidden<ExpenseBillDeleteResponseDto>();
        try
        {
            var data = await service.DeleteDraftAsync(apartmentId, billId, cancellationToken);
            return Ok(new ApiResponseDto<ExpenseBillDeleteResponseDto> { Success = true, Message = "Expense bill deleted.", Data = data });
        }
        catch (UnauthorizedAccessException) { return ForbidResponse<ExpenseBillDeleteResponseDto>(); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiResponseDto<ExpenseBillDeleteResponseDto> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] }); }
        catch (Exception ex) { logger.LogError(ex, "Delete expense bill {BillId}.", billId); return ServerError<ExpenseBillDeleteResponseDto>(); }
    }

    [HttpGet("bills/{billId:long}/attachment")]
    public async Task<IActionResult> Attachment([FromRoute] long billId, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            var data = await service.DownloadAttachmentAsync(apartmentId, billId, cancellationToken);
            return File(data.Content, data.ContentType, data.FileName);
        }
        catch (UnauthorizedAccessException) { return StatusCode(StatusCodes.Status403Forbidden); }
        catch (InvalidOperationException ex) { return NotFound(ex.Message); }
        catch (Exception ex) { logger.LogError(ex, "Download expense attachment {BillId}.", billId); return StatusCode(StatusCodes.Status500InternalServerError); }
    }

    [HttpPost("bills/calculate")]
    public async Task<ActionResult<ApiResponseDto<ExpenseCalculateResponseDto>>> Calculate([FromBody] ExpenseCalculateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await service.CalculateAsync(request, cancellationToken);
            return Ok(new ApiResponseDto<ExpenseCalculateResponseDto> { Success = true, Message = "Expense totals calculated.", Data = data });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiResponseDto<ExpenseCalculateResponseDto> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] }); }
        catch (Exception ex) { logger.LogError(ex, "Calculate expense totals."); return ServerError<ExpenseCalculateResponseDto>(); }
    }

    [HttpGet("budget-check")]
    public async Task<ActionResult<ApiResponseDto<ExpenseBudgetCheckDto>>> BudgetCheck([FromQuery] int expenseHeadId, [FromQuery] int fiscalYearId, [FromQuery] decimal? additionalAmount, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return Forbidden<ExpenseBudgetCheckDto>();
        try
        {
            var data = await service.BudgetCheckAsync(apartmentId, expenseHeadId, fiscalYearId, additionalAmount, cancellationToken);
            return Ok(new ApiResponseDto<ExpenseBudgetCheckDto> { Success = true, Message = "Budget utilisation loaded.", Data = data });
        }
        catch (UnauthorizedAccessException) { return ForbidResponse<ExpenseBudgetCheckDto>(); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiResponseDto<ExpenseBudgetCheckDto> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] }); }
        catch (Exception ex) { logger.LogError(ex, "Expense budget check."); return ServerError<ExpenseBudgetCheckDto>(); }
    }

    [HttpGet("bills/export")]
    public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] string? search, [FromQuery] int? expenseHeadId, [FromQuery] string? statusCode, [FromQuery] int? vendorId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            var data = await service.ExportBillsAsync(apartmentId, format, search, expenseHeadId, statusCode, vendorId, fromDate, toDate, cancellationToken);
            return File(data.Content, data.ContentType, data.FileName);
        }
        catch (UnauthorizedAccessException) { return StatusCode(StatusCodes.Status403Forbidden); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { logger.LogError(ex, "Export expense bills."); return StatusCode(StatusCodes.Status500InternalServerError); }
    }

    private ActionResult<ApiResponseDto<T>> Forbidden<T>() =>
        StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<T> { Success = false, Message = "Apartment context is required.", Errors = ["NO_APARTMENT_CONTEXT"] });

    private ActionResult<ApiResponseDto<T>> ForbidResponse<T>() =>
        StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<T> { Success = false, Message = "FORBIDDEN", Errors = ["FORBIDDEN"] });

    private ActionResult<ApiResponseDto<T>> ServerError<T>() =>
        StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDto<T> { Success = false, Message = "An unexpected error occurred.", Errors = ["INTERNAL_SERVER_ERROR"] });
}
