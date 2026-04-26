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
public sealed class ExpenseHeadsController(
    IExpenseHeadService service,
    ICurrentUser currentUser,
    ILogger<ExpenseHeadsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<ExpenseHeadDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<ExpenseHeadDto>>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<ExpenseHeadDto>>>> GetExpenseHeads(
        CancellationToken cancellationToken)
    {
        if (currentUser.IdApartment is not { } apartmentId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponseDto<IReadOnlyList<ExpenseHeadDto>>
                {
                    Success = false,
                    Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                    Errors = ["NO_APARTMENT_CONTEXT"]
                });
        }
        try
        {
            var data = await service.ListExpenseHeadsForApartmentAsync(apartmentId, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<ExpenseHeadDto>>
            {
                Success = true,
                Message = "Expense heads loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetExpenseHeads failed.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<IReadOnlyList<ExpenseHeadDto>>
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    Errors = ["INTERNAL_SERVER_ERROR"]
                });
        }
    }

    [HttpGet("{idExpenseHead:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<ExpenseHeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<ExpenseHeadDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<ExpenseHeadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<ExpenseHeadDto>>> GetByIdExpenseHead(
        [FromRoute] int idExpenseHead,
        CancellationToken cancellationToken)
    {
        if (idExpenseHead <= 0)
        {
            return BadRequest(new ApiResponseDto<ExpenseHeadDto>
            {
                Success = false,
                Message = "idExpenseHead must be positive."
            });
        }
        if (currentUser.IdApartment is not { } apartmentId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponseDto<ExpenseHeadDto>
                {
                    Success = false,
                    Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                    Errors = ["NO_APARTMENT_CONTEXT"]
                });
        }
        try
        {
            var data = await service.GetByIdAsync(idExpenseHead, apartmentId, cancellationToken);
            if (data is null)
            {
                return NotFound(new ApiResponseDto<ExpenseHeadDto>
                {
                    Success = false,
                    Message = "Expense head not found."
                });
            }
            return Ok(new ApiResponseDto<ExpenseHeadDto>
            {
                Success = true,
                Message = "Expense head loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetById expense head {IdExpenseHead}.", idExpenseHead);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<ExpenseHeadDto>
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    Errors = ["INTERNAL_SERVER_ERROR"]
                });
        }
    }

    /// <summary>
    /// Single save: <c>IdExpenseHead = 0</c> to create; set id and other fields in body to update. <c>ApartmentId</c> is set from the access token.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<ExpenseHeadDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseDto<ExpenseHeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<ExpenseHeadDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<ExpenseHeadDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<ExpenseHeadDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<ExpenseHeadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<ExpenseHeadDto>>> Save(
        [FromBody] ExpenseHeadSaveDto request,
        CancellationToken cancellationToken)
    {
        if (currentUser.IdUser is not { } userId)
        {
            return Unauthorized(new ApiResponseDto<ExpenseHeadDto>
            {
                Success = false,
                Message = "User id is not available in the token."
            });
        }
        if (currentUser.IdApartment is not { } apartmentId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponseDto<ExpenseHeadDto>
                {
                    Success = false,
                    Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                    Errors = ["NO_APARTMENT_CONTEXT"]
                });
        }
        request.ApartmentId = apartmentId;
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponseDto<ExpenseHeadDto>
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
                return NotFound(new ApiResponseDto<ExpenseHeadDto>
                {
                    Success = false,
                    Message = "Expense head not found for update (check id)."
                });
            }
            var payload = new ApiResponseDto<ExpenseHeadDto>
            {
                Success = true,
                Message = result.Created ? "Expense head created." : "Expense head updated.",
                Data = result.Data
            };
            if (result.Created)
                return StatusCode(StatusCodes.Status201Created, payload);
            return Ok(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Save expense head.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<ExpenseHeadDto>
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    Errors = ["INTERNAL_SERVER_ERROR"]
                });
        }
    }
}
