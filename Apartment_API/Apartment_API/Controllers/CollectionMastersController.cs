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
[Route("api/v{version:apiVersion}/masters")]
public sealed class CollectionMastersController(
    ICollectionsService service,
    ICurrentUser currentUser,
    ILogger<CollectionMastersController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("payment-modes")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterLookupItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<MasterLookupItemDto>>>> PaymentModes(CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await service.GetPaymentModesAsync(cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<MasterLookupItemDto>> { Success = true, Message = "Payment modes loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Payment modes.");
            return this.ApiServerError<IReadOnlyList<MasterLookupItemDto>>(environment, configuration, ex);
        }
    }

    [HttpGet("income-heads")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterLookupItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<MasterLookupItemDto>>>> IncomeHeads(CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return ForbiddenNoApartment<IReadOnlyList<MasterLookupItemDto>>();
        try
        {
            var data = await service.GetIncomeHeadsAsync(apartmentId, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<MasterLookupItemDto>> { Success = true, Message = "Income heads loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Income heads.");
            return this.ApiServerError<IReadOnlyList<MasterLookupItemDto>>(environment, configuration, ex);
        }
    }

    [HttpGet("invoice-statuses")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MasterLookupItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<MasterLookupItemDto>>>> InvoiceStatuses(CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await service.GetInvoiceStatusesAsync(cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<MasterLookupItemDto>> { Success = true, Message = "Invoice statuses loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invoice statuses.");
            return this.ApiServerError<IReadOnlyList<MasterLookupItemDto>>(environment, configuration, ex);
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
