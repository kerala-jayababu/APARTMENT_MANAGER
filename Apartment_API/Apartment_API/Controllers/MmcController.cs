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
[Route("api/v{version:apiVersion}/mmc")]
public sealed class MmcController(
    IMmcService service,
    ICurrentUser currentUser,
    ILogger<MmcController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("{mmcPeriodId:int}/units")]
    [ProducesResponseType(typeof(ApiResponseDto<MmcGridDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<MmcGridDto>>> GetGrid(
        [FromRoute] int mmcPeriodId,
        [FromQuery] string? search,
        [FromQuery] int? blockId,
        [FromQuery] int? unitTypeId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return Forbidden<MmcGridDto>();
        try
        {
            var data = await service.GetGridAsync(apartmentId, mmcPeriodId, search, blockId, unitTypeId, cancellationToken);
            return Ok(new ApiResponseDto<MmcGridDto> { Success = true, Message = "MMC grid loaded.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<MmcGridDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get MMC grid.");
            return this.ApiServerError<MmcGridDto>(environment, configuration, ex);
        }
    }

    [HttpPost("batches")]
    [ProducesResponseType(typeof(ApiResponseDto<MmcBatchCreatedDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<MmcBatchCreatedDto>>> SubmitBatch(
        [FromBody] SubmitMmcBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<MmcBatchCreatedDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId) return Forbidden<MmcBatchCreatedDto>();
        try
        {
            var data = await service.SubmitBatchAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseDto<MmcBatchCreatedDto> { Success = true, Message = "MMC batch submitted.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<MmcBatchCreatedDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Submit MMC batch.");
            return this.ApiServerError<MmcBatchCreatedDto>(environment, configuration, ex);
        }
    }

    [HttpGet("batches")]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResult<MmcBatchListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<PagedResult<MmcBatchListDto>>>> GetBatches(
        [FromQuery] int? mmcPeriodId,
        [FromQuery] string? statusCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return Forbidden<PagedResult<MmcBatchListDto>>();
        try
        {
            var data = await service.ListBatchesAsync(apartmentId, mmcPeriodId, statusCode, page, pageSize, cancellationToken);
            return Ok(new ApiResponseDto<PagedResult<MmcBatchListDto>> { Success = true, Message = "MMC batches loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "List MMC batches.");
            return this.ApiServerError<PagedResult<MmcBatchListDto>>(environment, configuration, ex);
        }
    }

    [HttpGet("batches/{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<MmcBatchDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<MmcBatchDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<MmcBatchDetailDto>>> GetBatch(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return Forbidden<MmcBatchDetailDto>();
        try
        {
            var data = await service.GetBatchAsync(apartmentId, id, cancellationToken);
            if (data is null) return NotFound(new ApiResponseDto<MmcBatchDetailDto> { Success = false, Message = "MMC batch not found." });
            return Ok(new ApiResponseDto<MmcBatchDetailDto> { Success = true, Message = "MMC batch loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get MMC batch {Id}.", id);
            return this.ApiServerError<MmcBatchDetailDto>(environment, configuration, ex);
        }
    }

    [HttpPost("batches/{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Approve(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<object?> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponseDto<object?> { Success = false, Message = "Apartment context is required.", Errors = ["NO_APARTMENT_CONTEXT"] });
        try
        {
            await service.ApproveBatchAsync(apartmentId, userId, id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<object?> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Approve MMC batch {Id}.", id);
            return this.ApiServerErrorAction<object?>(environment, configuration, ex);
        }
    }

    [HttpPost("batches/{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Reject(
        [FromRoute] int id,
        [FromBody] RejectRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<object?> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponseDto<object?> { Success = false, Message = "Apartment context is required.", Errors = ["NO_APARTMENT_CONTEXT"] });
        try
        {
            await service.RejectBatchAsync(apartmentId, userId, id, request, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<object?> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reject MMC batch {Id}.", id);
            return this.ApiServerErrorAction<object?>(environment, configuration, ex);
        }
    }

    [HttpPost("{mmcPeriodId:int}/copy-from/{sourcePeriodId:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<MmcGridDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<MmcGridDto>>> CopyFrom(
        [FromRoute] int mmcPeriodId,
        [FromRoute] int sourcePeriodId,
        [FromQuery] string? search,
        [FromQuery] int? blockId,
        [FromQuery] int? unitTypeId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return Forbidden<MmcGridDto>();
        try
        {
            var data = await service.CopyFromPeriodAsync(apartmentId, mmcPeriodId, sourcePeriodId, search, blockId, unitTypeId, cancellationToken);
            return Ok(new ApiResponseDto<MmcGridDto> { Success = true, Message = "MMC grid copied from source period.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<MmcGridDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Copy MMC from source period.");
            return this.ApiServerError<MmcGridDto>(environment, configuration, ex);
        }
    }

    private ActionResult<ApiResponseDto<T>> Forbidden<T>() =>
        StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponseDto<T> { Success = false, Message = "Apartment context is required. Use a tenant access token with apartment_id.", Errors = ["NO_APARTMENT_CONTEXT"] });

}
