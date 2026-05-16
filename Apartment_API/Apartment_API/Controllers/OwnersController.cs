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
[Route("api/v{version:apiVersion}/owners")]
public sealed class OwnersController(
    IOwnerResidentService service,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResult<OwnerListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<PagedResult<OwnerListDto>>>> GetList(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<PagedResult<OwnerListDto>>();
        var data = await service.ListAsync(apartmentId, search, isActive, page, pageSize, cancellationToken);
        return Ok(new ApiResponseDto<PagedResult<OwnerListDto>> { Success = true, Message = "Owners loaded.", Data = data });
    }

    [HttpGet("{personId:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<OwnerDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<OwnerDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<OwnerDetailDto>>> GetById(
        [FromRoute] int personId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<OwnerDetailDto>();
        var data = await service.GetAsync(apartmentId, personId, cancellationToken);
        if (data is null)
            return NotFound(new ApiResponseDto<OwnerDetailDto> { Success = false, Message = "Owner not found." });
        return Ok(new ApiResponseDto<OwnerDetailDto> { Success = true, Message = "Owner loaded.", Data = data });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<IdResultDto>>> Create(
        [FromBody] CreateOwnerRequest request,
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
                new ApiResponseDto<IdResultDto> { Success = true, Message = "Owner created.", Data = new IdResultDto { Id = personId } });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IdResultDto> { Success = false, Message = ex.Message });
        }
    }

    [HttpPut("{personId:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        [FromRoute] int personId,
        [FromBody] CreateOwnerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized();
        if (currentUser.IdApartment is not { } apartmentId)
            return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            await service.UpdateAsync(apartmentId, userId, personId, request, cancellationToken);
            return Ok(new ApiResponseDto<string> { Success = true, Message = "Owner updated.", Data = "UPDATED" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{personId:int}/id-proof")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponseDto<IdProofResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IdProofResultDto>>> UploadIdProof(
        [FromRoute] int personId,
        IFormFile? file,
        [FromForm] int documentCategoryId,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ApiResponseDto<IdProofResultDto> { Success = false, Message = "file is required." });
        if (documentCategoryId <= 0)
            return BadRequest(new ApiResponseDto<IdProofResultDto>
            {
                Success = false,
                Message = "documentCategoryId is required and must be a positive value (e.g. 2 for Identity Proof)."
            });
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<IdProofResultDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<IdProofResultDto>();
        try
        {
            await using var stream = file.OpenReadStream();
            var data = await service.UploadIdProofAsync(
                apartmentId, userId, personId, stream, file.FileName, documentCategoryId, cancellationToken);
            return Ok(new ApiResponseDto<IdProofResultDto> { Success = true, Message = "ID proof uploaded.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IdProofResultDto> { Success = false, Message = ex.Message });
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
