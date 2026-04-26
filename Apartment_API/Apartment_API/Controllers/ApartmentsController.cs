using Asp.Versioning;
using Apartment_API.DTO;
using Apartment_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Apartment_API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ApartmentsController(
    IApartmentService apartmentService,
    ILogger<ApartmentsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyCollection<ApartmentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyCollection<ApartmentDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyCollection<ApartmentDto>>>> GetAll(
        CancellationToken cancellationToken)
    {
        try
        {
            var data = await apartmentService.GetAllAsync(cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyCollection<ApartmentDto>>
            {
                Success = true,
                Message = "Apartments loaded successfully.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while getting apartments.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<IReadOnlyCollection<ApartmentDto>>
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    Errors = ["INTERNAL_SERVER_ERROR"]
                });
        }
    }
}
