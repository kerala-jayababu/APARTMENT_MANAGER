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
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class VendorsController(
    IVendorService service,
    ICurrentUser currentUser,
    ILogger<VendorsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<VendorDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<VendorDto>>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<VendorDto>>>> GetVendors(
        CancellationToken cancellationToken)
    {
        if (currentUser.IdApartment is not { } apartmentId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponseDto<IReadOnlyList<VendorDto>>
                {
                    Success = false,
                    Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                    Errors = ["NO_APARTMENT_CONTEXT"]
                });
        }
        try
        {
            var data = await service.ListVendorsForApartmentAsync(apartmentId, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<VendorDto>>
            {
                Success = true,
                Message = "Vendors loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetVendors failed.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<IReadOnlyList<VendorDto>>
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    Errors = ["INTERNAL_SERVER_ERROR"]
                });
        }
    }

    [HttpGet("{idVendor:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<VendorDto>>> GetByIdVendor(
        [FromRoute] int idVendor,
        CancellationToken cancellationToken)
    {
        if (idVendor <= 0)
        {
            return BadRequest(new ApiResponseDto<VendorDto>
            {
                Success = false,
                Message = "idVendor must be positive."
            });
        }
        if (currentUser.IdApartment is not { } apartmentId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponseDto<VendorDto>
                {
                    Success = false,
                    Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                    Errors = ["NO_APARTMENT_CONTEXT"]
                });
        }
        try
        {
            var data = await service.GetByIdAsync(idVendor, apartmentId, cancellationToken);
            if (data is null)
            {
                return NotFound(new ApiResponseDto<VendorDto>
                {
                    Success = false,
                    Message = "Vendor not found."
                });
            }
            return Ok(new ApiResponseDto<VendorDto>
            {
                Success = true,
                Message = "Vendor loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetById vendor {IdVendor}.", idVendor);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<VendorDto>
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    Errors = ["INTERNAL_SERVER_ERROR"]
                });
        }
    }

    /// <summary>
    /// Single save: <c>IdVendor = 0</c> to create, or the existing id to update. <c>ApartmentId</c> is taken from the access token (client value is ignored).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<VendorDto>>> Save(
        [FromBody] VendorSaveDto request,
        CancellationToken cancellationToken)
    {
        if (currentUser.IdUser is not { } userId)
        {
            return Unauthorized(new ApiResponseDto<VendorDto>
            {
                Success = false,
                Message = "User id is not available in the token."
            });
        }
        if (currentUser.IdApartment is not { } apartmentId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponseDto<VendorDto>
                {
                    Success = false,
                    Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                    Errors = ["NO_APARTMENT_CONTEXT"]
                });
        }
        request.ApartmentId = apartmentId;
        ModelState.Clear();
        if (!TryValidateModel(request))
        {
            return BadRequest(new ApiResponseDto<VendorDto>
            {
                Success = false,
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }
        try
        {
            var result = await service.SaveAsync(request, userId, apartmentId, cancellationToken);
            if (result is null)
            {
                return NotFound(new ApiResponseDto<VendorDto>
                {
                    Success = false,
                    Message = "Vendor not found for update (check id and apartment)."
                });
            }
            var payload = new ApiResponseDto<VendorDto>
            {
                Success = true,
                Message = result.Created ? "Vendor created." : "Vendor updated.",
                Data = result.Data
            };
            if (result.Created)
                return StatusCode(StatusCodes.Status201Created, payload);
            return Ok(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Save vendor.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<VendorDto>
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    Errors = ["INTERNAL_SERVER_ERROR"]
                });
        }
    }
}
