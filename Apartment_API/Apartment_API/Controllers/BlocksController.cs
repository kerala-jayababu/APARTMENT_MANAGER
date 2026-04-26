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
[Route("api/v{version:apiVersion}/blocks")]
public sealed class BlocksController(
    IBlockService service,
    ICurrentUser currentUser,
    ILogger<BlocksController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<BlockListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<BlockListDto>>>> GetList(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<IReadOnlyList<BlockListDto>>();
        try
        {
            var data = await service.ListAsync(apartmentId, search, isActive, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<BlockListDto>>
            {
                Success = true,
                Message = "Blocks loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetList blocks.");
            return ServerError<IReadOnlyList<BlockListDto>>();
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<BlockListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<BlockListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<BlockListDto>>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<BlockListDto>();
        try
        {
            var data = await service.GetByIdAsync(apartmentId, id, cancellationToken);
            if (data is null)
                return NotFound(new ApiResponseDto<BlockListDto> { Success = false, Message = "Block not found." });
            return Ok(new ApiResponseDto<BlockListDto> { Success = true, Message = "Block loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetById block {Id}.", id);
            return ServerError<BlockListDto>();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<IdResultDto>>> Create(
        [FromBody] CreateBlockRequest request,
        CancellationToken cancellationToken)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<IdResultDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<IdResultDto>();
        try
        {
            var id = await service.CreateAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponseDto<IdResultDto>
                {
                    Success = true,
                    Message = "Block created.",
                    Data = new IdResultDto { Id = id }
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IdResultDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create block.");
            return ServerError<IdResultDto>();
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] CreateBlockRequest request,
        CancellationToken cancellationToken)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized();
        if (currentUser.IdApartment is not { } apartmentId)
            return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            await service.UpdateAsync(apartmentId, userId, id, request, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update block {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            await service.DeleteAsync(apartmentId, id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("active units", StringComparison.OrdinalIgnoreCase))
                return Conflict(ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delete block {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
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

    private ActionResult<ApiResponseDto<T>> ServerError<T>() =>
        StatusCode(StatusCodes.Status500InternalServerError,
            new ApiResponseDto<T>
            {
                Success = false,
                Message = "An unexpected error occurred.",
                Errors = ["INTERNAL_SERVER_ERROR"]
            });
}
