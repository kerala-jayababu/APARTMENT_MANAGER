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
[Route("api/v{version:apiVersion}/committee-tenures")]
public sealed class CommitteeTenuresController(
    ICommitteeTenureService tenureService,
    ICommitteeMemberService memberService,
    ICurrentUser currentUser,
    ILogger<CommitteeTenuresController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResult<CommitteeTenureListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<PagedResult<CommitteeTenureListDto>>>> GetList(
        [FromQuery] bool? includeArchived = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<PagedResult<CommitteeTenureListDto>>();
        try
        {
            var data = await tenureService.ListTenuresAsync(apartmentId, includeArchived, page, pageSize, cancellationToken);
            return Ok(new ApiResponseDto<PagedResult<CommitteeTenureListDto>> { Success = true, Message = "Tenures loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "List tenures.");
            return ServerError<PagedResult<CommitteeTenureListDto>>();
        }
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponseDto<CommitteeTenureDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<CommitteeTenureDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<CommitteeTenureDetailDto>>> GetActive(CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<CommitteeTenureDetailDto>();
        try
        {
            var data = await tenureService.GetActiveTenureAsync(apartmentId, cancellationToken);
            if (data is null)
                return NotFound(new ApiResponseDto<CommitteeTenureDetailDto> { Success = false, Message = "No active MC tenure." });
            return Ok(new ApiResponseDto<CommitteeTenureDetailDto> { Success = true, Message = "Active tenure loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get active tenure.");
            return ServerError<CommitteeTenureDetailDto>();
        }
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResult<CommitteeTenureHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<PagedResult<CommitteeTenureHistoryDto>>>> GetHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<PagedResult<CommitteeTenureHistoryDto>>();
        try
        {
            var data = await tenureService.GetTenureHistoryPageAsync(apartmentId, from, to, page, pageSize, cancellationToken);
            return Ok(new ApiResponseDto<PagedResult<CommitteeTenureHistoryDto>> { Success = true, Message = "History loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tenure history.");
            return ServerError<PagedResult<CommitteeTenureHistoryDto>>();
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<CommitteeTenureDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<CommitteeTenureDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<CommitteeTenureDetailDto>>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<CommitteeTenureDetailDto>();
        try
        {
            var data = await tenureService.GetTenureByIdAsync(apartmentId, id, cancellationToken);
            if (data is null)
                return NotFound(new ApiResponseDto<CommitteeTenureDetailDto> { Success = false, Message = "Tenure not found." });
            return Ok(new ApiResponseDto<CommitteeTenureDetailDto> { Success = true, Message = "Tenure loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get tenure {Id}.", id);
            return ServerError<CommitteeTenureDetailDto>();
        }
    }

    [HttpGet("{id:int}/extensions")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<TenureExtensionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<TenureExtensionDto>>>> GetExtensions(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<IReadOnlyList<TenureExtensionDto>>();
        try
        {
            var data = await tenureService.GetExtensionsForTenureAsync(apartmentId, id, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<TenureExtensionDto>> { Success = true, Message = "Extensions loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get extensions {Id}.", id);
            return ServerError<IReadOnlyList<TenureExtensionDto>>();
        }
    }

    [HttpGet("{id:int}/members")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<CommitteeMemberListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<CommitteeMemberListDto>>>> GetMembersForTenure(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<IReadOnlyList<CommitteeMemberListDto>>();
        try
        {
            var data = await memberService.GetMembersForTenureAsync(apartmentId, id, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<CommitteeMemberListDto>> { Success = true, Message = "Members loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get members for tenure {Id}.", id);
            return ServerError<IReadOnlyList<CommitteeMemberListDto>>();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<IdResultDto>>> Create(
        [FromBody] CreateCommitteeTenureRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<IdResultDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<IdResultDto>();
        try
        {
            var id = await tenureService.CreateTenureAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponseDto<IdResultDto> { Success = true, Message = "Tenure created.", Data = new IdResultDto { Id = id } });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IdResultDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create tenure.");
            return ServerError<IdResultDto>();
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] CreateCommitteeTenureRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId) return Unauthorized();
        if (currentUser.IdApartment is not { } apartmentId) return Forbid();
        try
        {
            await tenureService.UpdateTenureAsync(apartmentId, userId, id, request, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update tenure {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("{id:int}/extend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Extend(
        [FromRoute] int id,
        [FromBody] ExtendTenureRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId) return Unauthorized();
        if (currentUser.IdApartment is not { } apartmentId) return Forbid();
        try
        {
            await tenureService.ExtendTenureAsync(apartmentId, userId, id, request, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Extend tenure {Id}.", id);
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
