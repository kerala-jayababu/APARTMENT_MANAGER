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
[Route("api/v{version:apiVersion}/module-groups")]
public sealed class ModuleGroupsController(
    IModuleGroupService moduleGroups,
    ILogger<ModuleGroupsController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>All rows from dbo.modulegroups (global master). No module RBAC header required.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<ModuleGroupListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<ModuleGroupListItemDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<ModuleGroupListItemDto>>>> GetList(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        try
        {
            var data = await moduleGroups.ListModuleGroupsAsync(isActive, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<ModuleGroupListItemDto>>
            {
                Success = true,
                Message = "Module groups loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetList module groups.");
            return this.ApiServerError<IReadOnlyList<ModuleGroupListItemDto>>(environment, configuration, ex);
        }
    }
}
