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
[Route("api/v{version:apiVersion}/amenity-bookings")]
public sealed class AmenityBookingsController(
    IAmenityService service,
    ICurrentUser currentUser,
    ILogger<AmenityBookingsController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<AmenityBookingListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<AmenityBookingListDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<AmenityBookingListDto>>>> GetList(
        [FromQuery] string? search,
        [FromQuery] int? amenityId,
        [FromQuery] int? statusId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<IReadOnlyList<AmenityBookingListDto>>();
        try
        {
            var data = await service.ListBookingsAsync(apartmentId, search, amenityId, statusId, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<AmenityBookingListDto>>
            {
                Success = true,
                Message = "Amenity bookings loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get amenity bookings.");
            return this.ApiServerError<IReadOnlyList<AmenityBookingListDto>>(environment, configuration, ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<IdResultDto>>> Create(
        [FromBody] CreateAmenityBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
        {
            return Unauthorized(new ApiResponseDto<IdResultDto>
            {
                Success = false,
                Message = "User id is not available in the token.",
                Errors = ["NO_USER_CONTEXT"]
            });
        }

        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<IdResultDto>();
        try
        {
            var id = await service.CreateBookingAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseDto<IdResultDto>
            {
                Success = true,
                Message = "Amenity booking created.",
                Data = new IdResultDto { Id = id }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IdResultDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = ["VALIDATION_FAILED"]
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create amenity booking.");
            return this.ApiServerError<IdResultDto>(environment, configuration, ex);
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<string>>> Update(
        [FromRoute] int id,
        [FromBody] CreateAmenityBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<string> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<string>();
        try
        {
            await service.UpdateBookingAsync(apartmentId, userId, id, request, cancellationToken);
            return Ok(new ApiResponseDto<string> { Success = true, Message = "Amenity booking updated.", Data = "UPDATED" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<string> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update amenity booking {Id}.", id);
            return this.ApiServerError<string>(environment, configuration, ex);
        }
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<string>>> Cancel(
        [FromRoute] int id,
        [FromBody] CancelAmenityBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<string> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<string>();
        try
        {
            await service.CancelBookingAsync(apartmentId, userId, id, request, cancellationToken);
            return Ok(new ApiResponseDto<string> { Success = true, Message = "Amenity booking cancelled.", Data = "CANCELLED" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<string> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cancel amenity booking {Id}.", id);
            return this.ApiServerError<string>(environment, configuration, ex);
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
