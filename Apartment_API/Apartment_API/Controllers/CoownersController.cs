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
[Route("api/v{version:apiVersion}/coowners")]
public sealed class CoownersController(
    ICoOwnerResidentService service,
    ICurrentUser currentUser,
    ILogger<CoownersController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResult<CoOwnerListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<PagedResult<CoOwnerListDto>>>> GetList(
        [FromQuery] int? primaryOwnerPersonId,
        [FromQuery] int? unitId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<PagedResult<CoOwnerListDto>>();
        try
        {
            var data = await service.ListAsync(
                apartmentId, primaryOwnerPersonId, unitId, isActive, page, pageSize, cancellationToken);
            return Ok(new ApiResponseDto<PagedResult<CoOwnerListDto>> { Success = true, Message = "Co-owners loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetList coowners.");
            return this.ApiServerError<PagedResult<CoOwnerListDto>>(environment, configuration, ex);
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<OwnerDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<OwnerDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<OwnerDetailDto>>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<OwnerDetailDto>();
        try
        {
            var data = await service.GetByUnitOwnerIdAsync(apartmentId, id, cancellationToken);
            if (data is null)
                return NotFound(new ApiResponseDto<OwnerDetailDto> { Success = false, Message = "Co-owner not found." });
            return Ok(new ApiResponseDto<OwnerDetailDto> { Success = true, Message = "Co-owner loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetById coowner {Id}.", id);
            return this.ApiServerError<OwnerDetailDto>(environment, configuration, ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<CoOwnerCreatedDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<CoOwnerCreatedDto>>> Create(
        [FromBody] CreateCoOwnerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<CoOwnerCreatedDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<CoOwnerCreatedDto>();
        try
        {
            var data = await service.CreateAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponseDto<CoOwnerCreatedDto> { Success = true, Message = "Co-owner created.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<CoOwnerCreatedDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create coowner.");
            return this.ApiServerError<CoOwnerCreatedDto>(environment, configuration, ex);
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] CreateCoOwnerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized();
        if (currentUser.IdApartment is not { } apartmentId)
            return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            await service.UpdateAsync(apartmentId, userId, id, request, cancellationToken);
            return Ok(new ApiResponseDto<string> { Success = true, Message = "Co-owner updated.", Data = "UPDATED" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update coowner {Id}.", id);
            return this.ApiServerErrorAction<object?>(environment, configuration, ex);
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized();
        if (currentUser.IdApartment is not { } apartmentId)
            return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            await service.DeleteAsync(apartmentId, id, userId, cancellationToken);
            return Ok(new ApiResponseDto<string> { Success = true, Message = "Co-owner deleted.", Data = "DELETED" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delete coowner {Id}.", id);
            return this.ApiServerErrorAction<object?>(environment, configuration, ex);
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
