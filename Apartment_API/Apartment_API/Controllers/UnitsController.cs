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
[Route("api/v{version:apiVersion}/units")]
public sealed class UnitsController(
    IUnitResidentService units,
    IOwnershipTransferResidentService ownership,
    IMmcService mmc,
    ICurrentUser currentUser,
    ILogger<UnitsController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResult<UnitListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<PagedResult<UnitListDto>>>> GetList(
        [FromQuery] string? search,
        [FromQuery] int? blockId,
        [FromQuery] int? unitTypeId,
        [FromQuery] int? unitStatusId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<PagedResult<UnitListDto>>();
        try
        {
            var data = await units.ListUnitsAsync(
                apartmentId, search, blockId, unitTypeId, unitStatusId, page, pageSize, cancellationToken);
            return Ok(new ApiResponseDto<PagedResult<UnitListDto>> { Success = true, Message = "Units loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetList units.");
            return this.ApiServerError<PagedResult<UnitListDto>>(environment, configuration, ex);
        }
    }

    [HttpGet("occupancy")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<BlockOccupancyDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<BlockOccupancyDto>>>> GetOccupancy(
        [FromQuery] int? blockId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<IReadOnlyList<BlockOccupancyDto>>();
        try
        {
            var data = await units.GetOccupancyAsync(apartmentId, blockId, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<BlockOccupancyDto>> { Success = true, Message = "Occupancy loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetOccupancy.");
            return this.ApiServerError<IReadOnlyList<BlockOccupancyDto>>(environment, configuration, ex);
        }
    }

    [HttpGet("status-history")]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResult<UnitStatusHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<PagedResult<UnitStatusHistoryDto>>>> GetStatusHistory(
        [FromQuery] int? unitId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<PagedResult<UnitStatusHistoryDto>>();
        try
        {
            var data = await units.GetStatusHistoryAsync(apartmentId, unitId, from, to, page, pageSize, cancellationToken);
            return Ok(new ApiResponseDto<PagedResult<UnitStatusHistoryDto>> { Success = true, Message = "Status history loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetStatusHistory.");
            return this.ApiServerError<PagedResult<UnitStatusHistoryDto>>(environment, configuration, ex);
        }
    }

    [HttpPost("bulk-generate")]
    [ProducesResponseType(typeof(ApiResponseDto<BulkGenerateResultDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<BulkGenerateResultDto>>> BulkGenerate(
        [FromBody] BulkGenerateUnitsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<BulkGenerateResultDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<BulkGenerateResultDto>();
        try
        {
            var data = await units.BulkGenerateAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponseDto<BulkGenerateResultDto> { Success = true, Message = "Units generated.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<BulkGenerateResultDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BulkGenerate.");
            return this.ApiServerError<BulkGenerateResultDto>(environment, configuration, ex);
        }
    }

    [HttpGet("{id:int}/status-history")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<UnitStatusHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<UnitStatusHistoryDto>>>> GetStatusHistoryForUnit(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<IReadOnlyList<UnitStatusHistoryDto>>();
        try
        {
            var data = await units.GetStatusHistoryForUnitAsync(apartmentId, id, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<UnitStatusHistoryDto>> { Success = true, Message = "Unit status history loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetStatusHistoryForUnit {Id}.", id);
            return this.ApiServerError<IReadOnlyList<UnitStatusHistoryDto>>(environment, configuration, ex);
        }
    }

    [HttpGet("{id:int}/ownership-history")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<OwnershipHistoryItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<OwnershipHistoryItemDto>>>> GetOwnershipHistory(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<IReadOnlyList<OwnershipHistoryItemDto>>();
        try
        {
            var data = await ownership.GetHistoryForUnitAsync(apartmentId, id, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<OwnershipHistoryItemDto>> { Success = true, Message = "Ownership history loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetOwnershipHistory {Id}.", id);
            return this.ApiServerError<IReadOnlyList<OwnershipHistoryItemDto>>(environment, configuration, ex);
        }
    }

    [HttpGet("{id:int}/mmc-history")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<UnitMmcHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<UnitMmcHistoryDto>>>> GetMmcHistory(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<IReadOnlyList<UnitMmcHistoryDto>>();
        try
        {
            var data = await mmc.GetUnitHistoryAsync(apartmentId, id, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<UnitMmcHistoryDto>> { Success = true, Message = "MMC history loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetMmcHistory {Id}.", id);
            return this.ApiServerError<IReadOnlyList<UnitMmcHistoryDto>>(environment, configuration, ex);
        }
    }

    [HttpPost("{id:int}/status")]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<IdResultDto>>> ChangeStatus(
        [FromRoute] int id,
        [FromBody] ChangeUnitStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<IdResultDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<IdResultDto>();
        try
        {
            var historyId = await units.ChangeStatusAsync(apartmentId, userId, id, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponseDto<IdResultDto> { Success = true, Message = "Status changed.", Data = new IdResultDto { Id = historyId } });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IdResultDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ChangeStatus {Id}.", id);
            return this.ApiServerError<IdResultDto>(environment, configuration, ex);
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<UnitDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<UnitDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<UnitDetailDto>>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<UnitDetailDto>();
        try
        {
            var data = await units.GetUnitAsync(apartmentId, id, cancellationToken);
            if (data is null)
                return NotFound(new ApiResponseDto<UnitDetailDto> { Success = false, Message = "Unit not found." });
            return Ok(new ApiResponseDto<UnitDetailDto> { Success = true, Message = "Unit loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetById unit {Id}.", id);
            return this.ApiServerError<UnitDetailDto>(environment, configuration, ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<IdResultDto>>> Create(
        [FromBody] CreateUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<IdResultDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<IdResultDto>();
        try
        {
            var id = await units.CreateUnitAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponseDto<IdResultDto> { Success = true, Message = "Unit created.", Data = new IdResultDto { Id = id } });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IdResultDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create unit.");
            return this.ApiServerError<IdResultDto>(environment, configuration, ex);
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<string>>> Update(
        [FromRoute] int id,
        [FromBody] CreateUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<string>
            {
                Success = false,
                Message = "User id is not available in the token.",
                Errors = ["UNAUTHORIZED"]
            });
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<string>();
        try
        {
            await units.UpdateUnitAsync(apartmentId, userId, id, request, cancellationToken);
            return Ok(new ApiResponseDto<string> { Success = true, Message = "Unit updated.", Data = "UPDATED" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<string>
            {
                Success = false,
                Message = ex.Message,
                Errors = ["VALIDATION_FAILED"]
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update unit {Id}.", id);
            return this.ApiServerError<string>(environment, configuration, ex);
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
