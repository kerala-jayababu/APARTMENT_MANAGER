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
public sealed class IncomeHeadsController(
    IIncomeHeadService service,
    ICurrentUser currentUser,
    ILogger<IncomeHeadsController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<IncomeHeadDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<IncomeHeadDto>>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<IncomeHeadDto>>>> GetIncomeHeads(
        CancellationToken cancellationToken)
    {
        if (currentUser.IdApartment is not { } apartmentId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponseDto<IReadOnlyList<IncomeHeadDto>>
                {
                    Success = false,
                    Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                    Errors = ["NO_APARTMENT_CONTEXT"]
                });
        }
        try
        {
            var data = await service.ListIncomeHeadsForApartmentAsync(apartmentId, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<IncomeHeadDto>>
            {
                Success = true,
                Message = "Income heads loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetIncomeHeads failed.");
            return this.ApiServerError<IReadOnlyList<IncomeHeadDto>>(environment, configuration, ex);
        }
    }

    [HttpGet("{idIncomeHead:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<IncomeHeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IncomeHeadDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<IncomeHeadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<IncomeHeadDto>>> GetByIdIncomeHead(
        [FromRoute] int idIncomeHead,
        CancellationToken cancellationToken)
    {
        if (idIncomeHead <= 0)
        {
            return BadRequest(new ApiResponseDto<IncomeHeadDto>
            {
                Success = false,
                Message = "idIncomeHead must be positive."
            });
        }
        if (currentUser.IdApartment is not { } apartmentId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponseDto<IncomeHeadDto>
                {
                    Success = false,
                    Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                    Errors = ["NO_APARTMENT_CONTEXT"]
                });
        }
        try
        {
            var data = await service.GetByIdAsync(idIncomeHead, apartmentId, cancellationToken);
            if (data is null)
            {
                return NotFound(new ApiResponseDto<IncomeHeadDto>
                {
                    Success = false,
                    Message = "Income head not found."
                });
            }
            return Ok(new ApiResponseDto<IncomeHeadDto>
            {
                Success = true,
                Message = "Income head loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetById income head {IdIncomeHead}.", idIncomeHead);
            return this.ApiServerError<IncomeHeadDto>(environment, configuration, ex);
        }
    }

    /// <summary>
    /// Single save: <c>IdIncomeHead = 0</c> to create; set id and other fields in body to update. <c>ApartmentId</c> is set from the access token.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<IncomeHeadDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseDto<IncomeHeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IncomeHeadDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<IncomeHeadDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IncomeHeadDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<IncomeHeadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<IncomeHeadDto>>> Save(
        [FromBody] IncomeHeadSaveDto request,
        CancellationToken cancellationToken)
    {
        if (currentUser.IdUser is not { } userId)
        {
            return Unauthorized(new ApiResponseDto<IncomeHeadDto>
            {
                Success = false,
                Message = "User id is not available in the token."
            });
        }
        if (currentUser.IdApartment is not { } apartmentId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponseDto<IncomeHeadDto>
                {
                    Success = false,
                    Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                    Errors = ["NO_APARTMENT_CONTEXT"]
                });
        }
        request.ApartmentId = apartmentId;
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponseDto<IncomeHeadDto>
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
                return NotFound(new ApiResponseDto<IncomeHeadDto>
                {
                    Success = false,
                    Message = "Income head not found for update (check id)."
                });
            }
            var payload = new ApiResponseDto<IncomeHeadDto>
            {
                Success = true,
                Message = result.Created ? "Income head created." : "Income head updated.",
                Data = result.Data
            };
            if (result.Created)
                return StatusCode(StatusCodes.Status201Created, payload);
            return Ok(payload);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IncomeHeadDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = ["VALIDATION_FAILED"]
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Save income head.");
            return this.ApiServerError<IncomeHeadDto>(environment, configuration, ex);
        }
    }
}
