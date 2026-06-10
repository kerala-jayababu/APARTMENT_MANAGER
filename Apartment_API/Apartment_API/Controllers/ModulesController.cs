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
[Route("api/v{version:apiVersion}/modules")]
public sealed class ModulesController(
    IModuleService modules,
    ILogger<ModulesController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>All rows from dbo.Modules (global master). No module RBAC header required.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<ModuleListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<ModuleListItemDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<ModuleListItemDto>>>> GetList(
        [FromQuery] bool? isActive,
        [FromQuery] string? moduleGroup,
        [FromQuery] string? parentModuleCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var data = await modules.ListModulesAsync(isActive, moduleGroup, parentModuleCode, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<ModuleListItemDto>>
            {
                Success = true,
                Message = "Modules loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetList modules.");
            return this.ApiServerError<IReadOnlyList<ModuleListItemDto>>(environment, configuration, ex);
        }
    }
}
