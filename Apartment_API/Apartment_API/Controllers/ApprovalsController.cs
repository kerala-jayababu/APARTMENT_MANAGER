using Asp.Versioning;
using Apartment_API.Configuration;
using Apartment_API.DTO;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Apartment_API.Controllers;

/// <summary>Unified approvals inbox (MMC batches, budgets, amenity bookings).</summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.ApiAccess)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/approvals")]
public sealed class ApprovalsController(
    IApprovalsInboxService inbox,
    ICurrentUser currentUser,
    ILogger<ApprovalsController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>Summary cards: totals per type + aging &gt; 3 days.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponseDto<ApprovalsInboxSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<ApprovalsInboxSummaryDto>>> GetSummary(CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<ApprovalsInboxSummaryDto>();
        try
        {
            var data = await inbox.GetSummaryAsync(apartmentId, cancellationToken);
            return Ok(new ApiResponseDto<ApprovalsInboxSummaryDto> { Success = true, Message = "Summary loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Approvals summary.");
            return this.ApiServerError<ApprovalsInboxSummaryDto>(environment, configuration, ex);
        }
    }

    /// <summary>
    /// Pending items for the inbox table. Use kind=all | set_mmc | budget | amenity_booking.
    /// Budget includes both annual budget headers and budget revisions awaiting approval.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResult<ApprovalInboxItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<PagedResult<ApprovalInboxItemDto>>>> GetPending(
        [FromQuery] string? kind,
        [FromQuery] DateOnly? submittedFrom,
        [FromQuery] DateOnly? submittedTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<PagedResult<ApprovalInboxItemDto>>();
        try
        {
            var data = await inbox.ListPendingAsync(apartmentId, kind, submittedFrom, submittedTo, page, pageSize, cancellationToken);
            return Ok(new ApiResponseDto<PagedResult<ApprovalInboxItemDto>>
                { Success = true, Message = "Approvals loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Approvals list.");
            return this.ApiServerError<PagedResult<ApprovalInboxItemDto>>(environment, configuration, ex);
        }
    }

    private ActionResult<ApiResponseDto<T>> Forbidden<T>() =>
        StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponseDto<T>
            {
                Success = false,
                Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                Errors = ["NO_APARTMENT_CONTEXT"]
            });
}
