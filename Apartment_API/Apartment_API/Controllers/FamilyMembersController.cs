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
[Route("api/v{version:apiVersion}/family-members")]
public sealed class FamilyMembersController(
    IFamilyMemberResidentService service,
    ICurrentUser currentUser,
    ILogger<FamilyMembersController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResult<FamilyMemberDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<PagedResult<FamilyMemberDto>>>> GetList(
        [FromQuery] int? unitId,
        [FromQuery] int? parentPersonId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<PagedResult<FamilyMemberDto>>();
        try
        {
            var data = await service.ListAsync(apartmentId, unitId, parentPersonId, page, pageSize, cancellationToken);
            return Ok(new ApiResponseDto<PagedResult<FamilyMemberDto>> { Success = true, Message = "Family members loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetList family members.");
            return this.ApiServerError<PagedResult<FamilyMemberDto>>(environment, configuration, ex);
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<FamilyMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<FamilyMemberDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<FamilyMemberDto>>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<FamilyMemberDto>();
        try
        {
            var data = await service.GetFamilyMemberAsync(apartmentId, id, cancellationToken);
            if (data is null)
                return NotFound(new ApiResponseDto<FamilyMemberDto> { Success = false, Message = "Family member not found." });
            return Ok(new ApiResponseDto<FamilyMemberDto> { Success = true, Message = "Family member loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetById family member {Id}.", id);
            return this.ApiServerError<FamilyMemberDto>(environment, configuration, ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<IdResultDto>>> Create(
        [FromBody] CreateFamilyMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<IdResultDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<IdResultDto>();
        try
        {
            var personId = await service.CreateAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponseDto<IdResultDto> { Success = true, Message = "Family member created.", Data = new IdResultDto { Id = personId } });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IdResultDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create family member.");
            return this.ApiServerError<IdResultDto>(environment, configuration, ex);
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] CreateFamilyMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized();
        if (currentUser.IdApartment is not { } apartmentId)
            return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            await service.UpdateAsync(apartmentId, userId, id, request, cancellationToken);
            return Ok(new ApiResponseDto<string> { Success = true, Message = "Family member updated.", Data = "UPDATED" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update family member {Id}.", id);
            return this.ApiServerErrorAction<object?>(environment, configuration, ex);
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            await service.DeleteAsync(apartmentId, id, cancellationToken);
            return Ok(new ApiResponseDto<string> { Success = true, Message = "Family member deleted.", Data = "DELETED" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delete family member {Id}.", id);
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
