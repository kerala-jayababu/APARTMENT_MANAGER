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
[Route("api/v{version:apiVersion}/budgets")]
public sealed class BudgetsController(
    IBudgetService service,
    ICurrentUser currentUser,
    ILogger<BudgetsController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("{fiscalYearId:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<BudgetDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<BudgetDetailDto>>> GetDetail(
        [FromRoute] int fiscalYearId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<BudgetDetailDto>();
        try
        {
            var data = await service.GetBudgetDetailAsync(apartmentId, fiscalYearId, cancellationToken);
            return Ok(new ApiResponseDto<BudgetDetailDto> { Success = true, Message = "Budget detail loaded.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<BudgetDetailDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get budget detail {FiscalYearId}.", fiscalYearId);
            return this.ApiServerError<BudgetDetailDto>(environment, configuration, ex);
        }
    }

    [HttpPut("{fiscalYearId:int}/lines")]
    [ProducesResponseType(typeof(ApiResponseDto<BudgetDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<BudgetDetailDto>>> SaveLines(
        [FromRoute] int fiscalYearId,
        [FromBody] SaveBudgetRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<BudgetDetailDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<BudgetDetailDto>();
        try
        {
            var data = await service.SaveBudgetLinesAsync(apartmentId, userId, fiscalYearId, request, cancellationToken);
            return Ok(new ApiResponseDto<BudgetDetailDto> { Success = true, Message = "Budget saved.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<BudgetDetailDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Save budget lines {FiscalYearId}.", fiscalYearId);
            return this.ApiServerError<BudgetDetailDto>(environment, configuration, ex);
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
