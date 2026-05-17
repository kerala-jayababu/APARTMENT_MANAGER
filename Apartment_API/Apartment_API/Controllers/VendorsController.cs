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
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class VendorsController(
    IVendorService service,
    ICurrentUser currentUser,
    ILogger<VendorsController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
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
            return this.ApiServerError<IReadOnlyList<VendorDto>>(environment, configuration, ex);
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
            return this.ApiServerError<VendorDto>(environment, configuration, ex);
        }
    }

    /// <summary>Create vendor. Set <c>IdVendor = 0</c> (or omit). <c>ApartmentId</c> is taken from the access token.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponseDto<VendorDto>>> Create(
        [FromBody] VendorSaveDto request,
        CancellationToken cancellationToken)
    {
        if (request.IdVendor > 0)
        {
            return BadRequest(new ApiResponseDto<VendorDto>
            {
                Success = false,
                Message = "Use PUT /api/v1/Vendors/{idVendor} to update an existing vendor.",
                Errors = ["USE_PUT_FOR_UPDATE"]
            });
        }
        return await SaveVendorAsync(request, expectCreate: true, cancellationToken);
    }

    [HttpPut("{idVendor:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<VendorDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<VendorDto>>> Update(
        [FromRoute] int idVendor,
        [FromBody] VendorSaveDto request,
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
        request.IdVendor = idVendor;
        return await SaveVendorAsync(request, expectCreate: false, cancellationToken);
    }

    private async Task<ActionResult<ApiResponseDto<VendorDto>>> SaveVendorAsync(
        VendorSaveDto request,
        bool expectCreate,
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
            if (expectCreate && !result.Created)
            {
                return BadRequest(new ApiResponseDto<VendorDto>
                {
                    Success = false,
                    Message = "Vendor was not created. Check IdVendor is 0 for POST."
                });
            }
            if (!expectCreate && result.Created)
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
            logger.LogError(ex, expectCreate ? "Create vendor." : "Update vendor {IdVendor}.", request.IdVendor);
            return this.ApiServerError<VendorDto>(environment, configuration, ex);
        }
    }
}
