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
[Route("api/v{version:apiVersion}/approval-rules")]
public sealed class ApprovalRulesController(
    IApprovalRuleService service,
    ILogger<ApprovalRulesController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<ApprovalRuleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<ApprovalRuleDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<ApprovalRuleDto>>>> GetList(CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await service.ListAsync(cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<ApprovalRuleDto>>
            {
                Success = true,
                Message = "Approval rules loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get approval rules.");
            return ServerError<IReadOnlyList<ApprovalRuleDto>>();
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<string>>> Update(
        [FromRoute] int id,
        [FromBody] UpdateApprovalRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await service.UpdateAsync(id, request, cancellationToken);
            return Ok(new ApiResponseDto<string>
            {
                Success = true,
                Message = "Approval rule updated.",
                Data = "UPDATED"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<string>
            {
                Success = false,
                Message = ex.Message,
                Errors = ["VALIDATION_FAILED"]
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update approval rule {Id}.", id);
            return ServerError<string>();
        }
    }

    private ActionResult<ApiResponseDto<T>> ServerError<T>() =>
        StatusCode(StatusCodes.Status500InternalServerError,
            new ApiResponseDto<T> { Success = false, Message = "An unexpected error occurred.", Errors = ["INTERNAL_SERVER_ERROR"] });
}
