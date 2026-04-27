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
[Route("api/v{version:apiVersion}/amenities")]
public sealed class AmenitiesController(
    IAmenityService service,
    ICurrentUser currentUser,
    ILogger<AmenitiesController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<AmenityListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<AmenityListDto>>>> GetList(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<IReadOnlyList<AmenityListDto>>();
        try
        {
            var data = await service.ListAsync(apartmentId, isActive, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<AmenityListDto>> { Success = true, Message = "Amenities loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetList amenities.");
            return ServerError<IReadOnlyList<AmenityListDto>>();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<IdResultDto>>> Create(
        [FromBody] CreateAmenityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<IdResultDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<IdResultDto>();
        try
        {
            var id = await service.CreateAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponseDto<IdResultDto> { Success = true, Message = "Amenity created.", Data = new IdResultDto { Id = id } });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IdResultDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create amenity.");
            return ServerError<IdResultDto>();
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] CreateAmenityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId) return Unauthorized();
        if (currentUser.IdApartment is not { } apartmentId) return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            await service.UpdateAsync(apartmentId, userId, id, request, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update amenity {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
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

    private ActionResult<ApiResponseDto<T>> ServerError<T>() =>
        StatusCode(StatusCodes.Status500InternalServerError,
            new ApiResponseDto<T> { Success = false, Message = "An unexpected error occurred.", Errors = ["INTERNAL_SERVER_ERROR"] });
}
