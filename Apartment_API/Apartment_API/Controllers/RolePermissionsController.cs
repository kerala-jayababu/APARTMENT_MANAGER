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
[Route("api/v{version:apiVersion}/role-permissions")]
public sealed class RolePermissionsController(
    IRolePermissionService rolePermissions,
    ICurrentUser currentUser,
    ILogger<RolePermissionsController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>All RolePermissions for the current apartment (optional filters). Not scoped to the caller's role.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<RolePermissionListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<RolePermissionListItemDto>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<RolePermissionListItemDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<RolePermissionListItemDto>>>> GetList(
        [FromQuery] int? roleId,
        [FromQuery] string? moduleCode,
        CancellationToken cancellationToken)
    {
        if (currentUser.IdApartment is not { } apartmentId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<IReadOnlyList<RolePermissionListItemDto>>
            {
                Success = false,
                Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                Errors = ["NO_APARTMENT_CONTEXT"]
            });
        }

        try
        {
            var data = await rolePermissions.ListRolePermissionsForApartmentAsync(
                apartmentId, roleId, moduleCode, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<RolePermissionListItemDto>>
            {
                Success = true,
                Message = "Role permissions loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetList role permissions.");
            return this.ApiServerError<IReadOnlyList<RolePermissionListItemDto>>(environment, configuration, ex);
        }
    }
}
