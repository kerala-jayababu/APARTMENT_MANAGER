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
public sealed class ApartmentsController(
    IApartmentService apartmentService,
    ICurrentUser currentUser,
    ILogger<ApartmentsController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyCollection<ApartmentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyCollection<ApartmentDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyCollection<ApartmentDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyCollection<ApartmentDto>>>> GetAll(
        CancellationToken cancellationToken)
    {
        try
        {
            // currentUser is filled from the JWT when [Authorize] succeeds (email, name, phone, id).
            var who = currentUser.Email ?? "unknown";
            var data = await apartmentService.GetAllAsync(cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyCollection<ApartmentDto>>
            {
                Success = true,
                Message = $"Apartments loaded for {who}.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while getting apartments.");
            return this.ApiServerError<IReadOnlyCollection<ApartmentDto>>(environment, configuration, ex);
        }
    }
}
