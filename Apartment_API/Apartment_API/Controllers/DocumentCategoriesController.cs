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
[Route("api/v{version:apiVersion}/document-categories")]
public sealed class DocumentCategoriesController(
    IDocumentService service,
    ILogger<DocumentCategoriesController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<DocumentCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<DocumentCategoryDto>>>> GetList(
        [FromQuery] bool isActive = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await service.ListCategoriesAsync(isActive, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<DocumentCategoryDto>>
            {
                Success = true,
                Message = "Document categories loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get document categories.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<IReadOnlyList<DocumentCategoryDto>>
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    Errors = ["INTERNAL_SERVER_ERROR"]
                });
        }
    }
}
