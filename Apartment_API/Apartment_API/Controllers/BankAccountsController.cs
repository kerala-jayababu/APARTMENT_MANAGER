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
public sealed class BankAccountsController(
    IBankAccountService service,
    ICurrentUser currentUser,
    ILogger<BankAccountsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<BankAccountDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<BankAccountDto>>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<BankAccountDto>>>> GetBankAccounts(
        CancellationToken cancellationToken)
    {
        if (currentUser.IdApartment is not { } apartmentId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponseDto<IReadOnlyList<BankAccountDto>>
                {
                    Success = false,
                    Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                    Errors = ["NO_APARTMENT_CONTEXT"]
                });
        }
        try
        {
            var data = await service.ListBankAccountsForApartmentAsync(apartmentId, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<BankAccountDto>>
            {
                Success = true,
                Message = "Bank accounts loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetBankAccounts failed.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<IReadOnlyList<BankAccountDto>>
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    Errors = ["INTERNAL_SERVER_ERROR"]
                });
        }
    }

    [HttpGet("{idBankAccount:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<BankAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<BankAccountDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<BankAccountDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<BankAccountDto>>> GetByIdBankAccount(
        [FromRoute] int idBankAccount,
        CancellationToken cancellationToken)
    {
        if (idBankAccount <= 0)
        {
            return BadRequest(new ApiResponseDto<BankAccountDto>
            {
                Success = false,
                Message = "idBankAccount must be positive."
            });
        }
        if (currentUser.IdApartment is not { } apartmentId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponseDto<BankAccountDto>
                {
                    Success = false,
                    Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                    Errors = ["NO_APARTMENT_CONTEXT"]
                });
        }
        try
        {
            var data = await service.GetByIdAsync(idBankAccount, apartmentId, cancellationToken);
            if (data is null)
            {
                return NotFound(new ApiResponseDto<BankAccountDto>
                {
                    Success = false,
                    Message = "Bank account not found."
                });
            }
            return Ok(new ApiResponseDto<BankAccountDto>
            {
                Success = true,
                Message = "Bank account loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetById bank account {IdBankAccount}.", idBankAccount);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<BankAccountDto>
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    Errors = ["INTERNAL_SERVER_ERROR"]
                });
        }
    }

    /// <summary>
    /// Single save: set <c>IdBankAccount = 0</c> to create, or the existing id to update. <c>ApartmentId</c> is taken from the access token (client value is ignored).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<BankAccountDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseDto<BankAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<BankAccountDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<BankAccountDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<BankAccountDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<BankAccountDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<BankAccountDto>>> Save(
        [FromBody] BankAccountSaveDto request,
        CancellationToken cancellationToken)
    {
        if (currentUser.IdUser is not { } userId)
        {
            return Unauthorized(new ApiResponseDto<BankAccountDto>
            {
                Success = false,
                Message = "User id is not available in the token."
            });
        }
        if (currentUser.IdApartment is not { } apartmentId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponseDto<BankAccountDto>
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
            return BadRequest(new ApiResponseDto<BankAccountDto>
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
                return NotFound(new ApiResponseDto<BankAccountDto>
                {
                    Success = false,
                    Message = "Bank account not found for update (check id and apartment)."
                });
            }
            var payload = new ApiResponseDto<BankAccountDto>
            {
                Success = true,
                Message = result.Created ? "Bank account created." : "Bank account updated.",
                Data = result.Data
            };
            if (result.Created)
                return StatusCode(StatusCodes.Status201Created, payload);
            return Ok(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Save bank account.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<BankAccountDto>
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    Errors = ["INTERNAL_SERVER_ERROR"]
                });
        }
    }
}
