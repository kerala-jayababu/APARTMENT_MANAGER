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
[Route("api/v{version:apiVersion}/ownership-transfers")]
public sealed class OwnershipTransfersController(
    IOwnershipTransferResidentService service,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<OwnershipTransferCreatedDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<OwnershipTransferCreatedDto>>> Record(
        [FromBody] RecordOwnershipTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<OwnershipTransferCreatedDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return ForbiddenNoApartment<OwnershipTransferCreatedDto>();
        try
        {
            var data = await service.RecordAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponseDto<OwnershipTransferCreatedDto> { Success = true, Message = "Ownership transfer recorded.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<OwnershipTransferCreatedDto> { Success = false, Message = ex.Message });
        }
    }

    [HttpPost("{id:long}/deed")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponseDto<IdProofResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IdProofResultDto>>> UploadDeed(
        [FromRoute] long id,
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
            var data = await service.UploadDeedAsync(apartmentId, userId, id, stream, file.FileName, cancellationToken);
            return Ok(new ApiResponseDto<IdProofResultDto> { Success = true, Message = "Deed uploaded.", Data = data });
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
