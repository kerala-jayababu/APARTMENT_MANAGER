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
[Route("api/v{version:apiVersion}/mmc-periods")]
public sealed class MmcPeriodsController(
    IMmcService service,
    ICurrentUser currentUser,
    ILogger<MmcPeriodsController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<MmcPeriodDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<MmcPeriodDto>>>> GetList(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<IReadOnlyList<MmcPeriodDto>>();
        try
        {
            var data = await service.ListPeriodsAsync(apartmentId, isActive, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<MmcPeriodDto>> { Success = true, Message = "MMC periods loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "List MMC periods.");
            return this.ApiServerError<IReadOnlyList<MmcPeriodDto>>(environment, configuration, ex);
        }
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponseDto<MmcPeriodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<MmcPeriodDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<MmcPeriodDto>>> GetCurrent(CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<MmcPeriodDto>();
        try
        {
            var data = await service.GetCurrentPeriodAsync(apartmentId, cancellationToken);
            if (data is null) return NotFound(new ApiResponseDto<MmcPeriodDto> { Success = false, Message = "Current MMC period not found." });
            return Ok(new ApiResponseDto<MmcPeriodDto> { Success = true, Message = "Current MMC period loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get current MMC period.");
            return this.ApiServerError<MmcPeriodDto>(environment, configuration, ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<IdResultDto>>> Create(
        [FromBody] CreateMmcPeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<IdResultDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<IdResultDto>();
        try
        {
            var id = await service.CreatePeriodAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponseDto<IdResultDto> { Success = true, Message = "MMC period created.", Data = new IdResultDto { Id = id } });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IdResultDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create MMC period.");
            return this.ApiServerError<IdResultDto>(environment, configuration, ex);
        }
    }

    private ActionResult<ApiResponseDto<T>> Forbidden<T>() =>
        StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponseDto<T> { Success = false, Message = "Apartment context is required. Use a tenant access token with apartment_id.",             Errors = ["NO_APARTMENT_CONTEXT"] });
}
