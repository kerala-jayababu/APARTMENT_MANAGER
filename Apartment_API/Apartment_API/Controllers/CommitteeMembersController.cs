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
[Route("api/v{version:apiVersion}/committee-members")]
public sealed class CommitteeMembersController(
    ICommitteeMemberService service,
    ICurrentUser currentUser,
    ILogger<CommitteeMembersController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResult<CommitteeMemberListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<PagedResult<CommitteeMemberListDto>>>> GetList(
        [FromQuery] int? committeeTenureId,
        [FromQuery] int? committeeRoleId,
        [FromQuery] string? statusCode,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<PagedResult<CommitteeMemberListDto>>();
        try
        {
            var data = await service.ListMembersAsync(
                apartmentId, committeeTenureId, committeeRoleId, statusCode, search, page, pageSize, cancellationToken);
            return Ok(new ApiResponseDto<PagedResult<CommitteeMemberListDto>> { Success = true, Message = "Members loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "List committee members.");
            return ServerError<PagedResult<CommitteeMemberListDto>>();
        }
    }

    [HttpGet("eligible-owners")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<EligibleOwnerDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<EligibleOwnerDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<EligibleOwnerDto>>>> GetEligibleOwners(
        [FromQuery] int committeeTenureId,
        [FromQuery] int? committeeRoleId,
        CancellationToken cancellationToken = default)
    {
        if (committeeTenureId <= 0)
            return BadRequest(new ApiResponseDto<IReadOnlyList<EligibleOwnerDto>> { Success = false, Message = "committeeTenureId is required." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<IReadOnlyList<EligibleOwnerDto>>();
        try
        {
            var data = await service.GetEligibleOwnersAsync(apartmentId, committeeTenureId, committeeRoleId, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<EligibleOwnerDto>> { Success = true, Message = "Eligible owners loaded.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IReadOnlyList<EligibleOwnerDto>> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Eligible owners.");
            return ServerError<IReadOnlyList<EligibleOwnerDto>>();
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<CommitteeMemberListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<CommitteeMemberListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<CommitteeMemberListDto>>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<CommitteeMemberListDto>();
        try
        {
            var data = await service.GetMemberByIdAsync(apartmentId, id, cancellationToken);
            if (data is null)
                return NotFound(new ApiResponseDto<CommitteeMemberListDto> { Success = false, Message = "Member not found." });
            return Ok(new ApiResponseDto<CommitteeMemberListDto> { Success = true, Message = "Member loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get member {Id}.", id);
            return ServerError<CommitteeMemberListDto>();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<IdResultDto>>> Assign(
        [FromBody] AssignCommitteeMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<IdResultDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<IdResultDto>();
        try
        {
            var id = await service.AssignMemberAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponseDto<IdResultDto> { Success = true, Message = "Member assigned.", Data = new IdResultDto { Id = id } });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IdResultDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Assign member.");
            return ServerError<IdResultDto>();
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateCommitteeMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId) return Unauthorized();
        if (currentUser.IdApartment is not { } apartmentId) return Forbid();
        try
        {
            await service.UpdateMemberAsync(apartmentId, userId, id, request, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update member {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("{id:int}/end")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> End(
        [FromRoute] int id,
        [FromBody] EndCommitteeMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId) return Unauthorized();
        if (currentUser.IdApartment is not { } apartmentId) return Forbid();
        try
        {
            await service.EndMemberAsync(apartmentId, userId, id, request, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception ex)
        {
            logger.LogError(ex, "End member {Id}.", id);
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
