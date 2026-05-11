using Asp.Versioning;
using Apartment_API.Configuration;
using Apartment_API.DTO;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Apartment_API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ApiAccess)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/collections")]
public sealed class CollectionsController(
    ICollectionsService service,
    ICurrentUser currentUser,
    ILogger<CollectionsController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponseDto<CollectionsSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<CollectionsSummaryDto>>> Summary([FromQuery] string? period, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return ForbiddenNoApartment<CollectionsSummaryDto>();
        try
        {
            var data = await service.GetSummaryAsync(apartmentId, period, cancellationToken);
            return Ok(new ApiResponseDto<CollectionsSummaryDto> { Success = true, Message = "Collections summary loaded.", Data = data });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<CollectionsSummaryDto> { Success = false, Message = "FORBIDDEN", Errors = ["FORBIDDEN"] });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<CollectionsSummaryDto> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Collections summary.");
            return this.ApiServerError<CollectionsSummaryDto>(environment, configuration, ex);
        }
    }

    [HttpGet("invoices")]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResult<CollectionInvoiceListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<PagedResult<CollectionInvoiceListItemDto>>>> ListInvoices(
        [FromQuery] string? search,
        [FromQuery] string? period,
        [FromQuery] int? incomeHeadId,
        [FromQuery] int? statusId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return ForbiddenNoApartment<PagedResult<CollectionInvoiceListItemDto>>();
        try
        {
            var data = await service.ListInvoicesAsync(apartmentId, search, period, incomeHeadId, statusId, pageNumber, pageSize, cancellationToken);
            return Ok(new ApiResponseDto<PagedResult<CollectionInvoiceListItemDto>> { Success = true, Message = "Invoices loaded.", Data = data });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<PagedResult<CollectionInvoiceListItemDto>> { Success = false, Message = "FORBIDDEN", Errors = ["FORBIDDEN"] });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<PagedResult<CollectionInvoiceListItemDto>> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Collections invoices.");
            return this.ApiServerError<PagedResult<CollectionInvoiceListItemDto>>(environment, configuration, ex);
        }
    }

    [HttpGet("invoices/{invoiceId:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<CollectionInvoiceDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<CollectionInvoiceDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<CollectionInvoiceDetailDto>>> GetInvoice([FromRoute] int invoiceId, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return ForbiddenNoApartment<CollectionInvoiceDetailDto>();
        try
        {
            var data = await service.GetInvoiceAsync(apartmentId, invoiceId, cancellationToken);
            if (data is null)
                return NotFound(new ApiResponseDto<CollectionInvoiceDetailDto> { Success = false, Message = "INVOICE_NOT_FOUND", Errors = ["INVOICE_NOT_FOUND"] });
            return Ok(new ApiResponseDto<CollectionInvoiceDetailDto> { Success = true, Message = "Invoice loaded.", Data = data });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<CollectionInvoiceDetailDto> { Success = false, Message = "FORBIDDEN", Errors = ["FORBIDDEN"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get invoice {InvoiceId}.", invoiceId);
            return this.ApiServerError<CollectionInvoiceDetailDto>(environment, configuration, ex);
        }
    }

    [HttpGet("invoices/export")]
    public async Task<IActionResult> ExportInvoices(
        [FromQuery] string format,
        [FromQuery] string? search,
        [FromQuery] string? period,
        [FromQuery] int? incomeHeadId,
        [FromQuery] int? statusId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            var data = await service.ExportInvoicesAsync(apartmentId, format, search, period, incomeHeadId, statusId, cancellationToken);
            return File(data.Content, data.ContentType, data.FileName);
        }
        catch (UnauthorizedAccessException) { return StatusCode(StatusCodes.Status403Forbidden); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Export invoices.");
            return this.ApiServerErrorAction<object?>(environment, configuration, ex);
        }
    }

    [HttpGet("quick-post/pending")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<QuickPostPendingItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<QuickPostPendingItemDto>>>> QuickPostPending(
        [FromQuery] string? search,
        [FromQuery] int? unitId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] int? incomeHeadId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return ForbiddenNoApartment<IReadOnlyList<QuickPostPendingItemDto>>();
        try
        {
            var data = await service.GetQuickPostPendingAsync(apartmentId, search, unitId, fromDate, incomeHeadId, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<QuickPostPendingItemDto>> { Success = true, Message = "Quick post pending invoices loaded.", Data = data });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<IReadOnlyList<QuickPostPendingItemDto>> { Success = false, Message = "FORBIDDEN", Errors = ["FORBIDDEN"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Quick post pending.");
            return this.ApiServerError<IReadOnlyList<QuickPostPendingItemDto>>(environment, configuration, ex);
        }
    }

    [HttpPost("quick-post/save")]
    [ProducesResponseType(typeof(ApiResponseDto<SaveQuickPostReceiptsResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<SaveQuickPostReceiptsResponseDto>>> QuickPostSave(
        [FromBody] SaveQuickPostReceiptsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<SaveQuickPostReceiptsResponseDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId) return ForbiddenNoApartment<SaveQuickPostReceiptsResponseDto>();
        try
        {
            var data = await service.SaveQuickPostReceiptsAsync(apartmentId, userId, request, cancellationToken);
            return Ok(new ApiResponseDto<SaveQuickPostReceiptsResponseDto> { Success = true, Message = "Receipts processed.", Data = data });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<SaveQuickPostReceiptsResponseDto> { Success = false, Message = "FORBIDDEN", Errors = ["FORBIDDEN"] });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<SaveQuickPostReceiptsResponseDto> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Quick post save.");
            return this.ApiServerError<SaveQuickPostReceiptsResponseDto>(environment, configuration, ex);
        }
    }

    [HttpGet("receipts/{receiptId:long}")]
    [ProducesResponseType(typeof(ApiResponseDto<ReceiptDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<ReceiptDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<ReceiptDetailDto>>> GetReceipt([FromRoute] long receiptId, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return ForbiddenNoApartment<ReceiptDetailDto>();
        try
        {
            var data = await service.GetReceiptAsync(apartmentId, receiptId, cancellationToken);
            if (data is null)
                return NotFound(new ApiResponseDto<ReceiptDetailDto> { Success = false, Message = "RECEIPT_NOT_FOUND", Errors = ["RECEIPT_NOT_FOUND"] });
            return Ok(new ApiResponseDto<ReceiptDetailDto> { Success = true, Message = "Receipt loaded.", Data = data });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<ReceiptDetailDto> { Success = false, Message = "FORBIDDEN", Errors = ["FORBIDDEN"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get receipt {ReceiptId}.", receiptId);
            return this.ApiServerError<ReceiptDetailDto>(environment, configuration, ex);
        }
    }

    [HttpDelete("receipts/{receiptId:long}")]
    [ProducesResponseType(typeof(ApiResponseDto<CancelReceiptResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<CancelReceiptResponseDto>>> CancelReceipt(
        [FromRoute] long receiptId,
        [FromBody] CancelReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<CancelReceiptResponseDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId) return ForbiddenNoApartment<CancelReceiptResponseDto>();
        try
        {
            var data = await service.CancelReceiptAsync(apartmentId, userId, receiptId, request, cancellationToken);
            return Ok(new ApiResponseDto<CancelReceiptResponseDto> { Success = true, Message = "Receipt cancelled.", Data = data });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<CancelReceiptResponseDto> { Success = false, Message = "FORBIDDEN", Errors = ["FORBIDDEN"] });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<CancelReceiptResponseDto> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cancel receipt {ReceiptId}.", receiptId);
            return this.ApiServerError<CancelReceiptResponseDto>(environment, configuration, ex);
        }
    }

    private ActionResult<ApiResponseDto<T>> ForbiddenNoApartment<T>() =>
        StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponseDto<T>
            {
                Success = false,
                Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                Errors = ["NO_APARTMENT_CONTEXT"]
            });
}
