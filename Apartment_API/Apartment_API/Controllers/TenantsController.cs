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
[Route("api/v{version:apiVersion}/tenants")]
public sealed class TenantsController(
    ITenantResidentService service,
    ICurrentUser currentUser,
    ILogger<TenantsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResult<TenantListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<PagedResult<TenantListDto>>>> GetList(
        [FromQuery] string? search,
        [FromQuery] int? unitId,
        [FromQuery] bool? isActive,
        [FromQuery] int? expiringWithinDays,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<PagedResult<TenantListDto>>();
        try
        {
            var data = await service.ListAsync(
                apartmentId, search, unitId, isActive, expiringWithinDays, page, pageSize, cancellationToken);
            return Ok(new ApiResponseDto<PagedResult<TenantListDto>> { Success = true, Message = "Tenants loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetList tenants.");
            return ServerError<PagedResult<TenantListDto>>();
        }
    }

    [HttpGet("expiring")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<TenantListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<TenantListDto>>>> GetExpiring(
        [FromQuery] int withinDays = 30,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<IReadOnlyList<TenantListDto>>();
        try
        {
            var data = await service.GetExpiringAsync(apartmentId, withinDays, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<TenantListDto>> { Success = true, Message = "Expiring leases loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetExpiring.");
            return ServerError<IReadOnlyList<TenantListDto>>();
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<TenantListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<TenantListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<TenantListDto>>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<TenantListDto>();
        try
        {
            var data = await service.GetByAssignmentIdAsync(apartmentId, id, cancellationToken);
            if (data is null)
                return NotFound(new ApiResponseDto<TenantListDto> { Success = false, Message = "Tenant not found." });
            return Ok(new ApiResponseDto<TenantListDto> { Success = true, Message = "Tenant loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetById tenant {Id}.", id);
            return ServerError<TenantListDto>();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<TenantCreatedDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<TenantCreatedDto>>> Create(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<TenantCreatedDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<TenantCreatedDto>();
        try
        {
            var data = await service.CreateAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponseDto<TenantCreatedDto> { Success = true, Message = "Tenant created.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<TenantCreatedDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create tenant.");
            return ServerError<TenantCreatedDto>();
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken = default)
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
            logger.LogError(ex, "Update tenant {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("{id:int}/vacate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Vacate(
        [FromRoute] int id,
        [FromBody] VacateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized();
        if (currentUser.IdApartment is not { } apartmentId)
            return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            await service.VacateAsync(apartmentId, userId, id, request, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Vacate tenant {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("{id:int}/lease-doc")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponseDto<IdProofResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IdProofResultDto>>> UploadLeaseDoc(
        [FromRoute] int id,
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ApiResponseDto<IdProofResultDto> { Success = false, Message = "file is required." });
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<IdProofResultDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<IdProofResultDto>();
        try
        {
            await using var stream = file.OpenReadStream();
            var data = await service.UploadLeaseDocumentAsync(apartmentId, userId, id, stream, file.FileName, cancellationToken);
            return Ok(new ApiResponseDto<IdProofResultDto> { Success = true, Message = "Lease document uploaded.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IdProofResultDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "UploadLeaseDoc {Id}.", id);
            return ServerError<IdProofResultDto>();
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
            new ApiResponseDto<T> { Success = false, Message = "An unexpected error occurred.", Errors = ["INTERNAL_SERVER_ERROR"] });
}
