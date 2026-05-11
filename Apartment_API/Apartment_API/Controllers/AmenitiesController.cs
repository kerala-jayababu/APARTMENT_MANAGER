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
[Route("api/v{version:apiVersion}/amenities")]
public sealed class AmenitiesController(
    IAmenityService service,
    ICurrentUser currentUser,
    ILogger<AmenitiesController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
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
            return this.ApiServerError<IReadOnlyList<AmenityListDto>>(environment, configuration, ex);
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
            return this.ApiServerError<IdResultDto>(environment, configuration, ex);
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<AmenityListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<AmenityListDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<AmenityListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<AmenityListDto>>> Update(
        [FromRoute] int id,
        [FromBody] CreateAmenityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<AmenityListDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<AmenityListDto>();
        try
        {
            await service.UpdateAsync(apartmentId, userId, id, request, cancellationToken);
            var data = await service.GetByIdAsync(apartmentId, id, cancellationToken);
            if (data is null)
            {
                return NotFound(new ApiResponseDto<AmenityListDto>
                    { Success = false, Message = "Amenity not found." });
            }

            return Ok(new ApiResponseDto<AmenityListDto>
                { Success = true, Message = "Amenity updated.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<AmenityListDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update amenity {Id}.", id);
            return this.ApiServerError<AmenityListDto>(environment, configuration, ex);
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
