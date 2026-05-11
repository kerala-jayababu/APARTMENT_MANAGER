using Apartment_API.DTO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Apartment_API.Configuration;

/// <summary>Builds <see cref="ApiResponseDto{T}"/> 500 responses using <see cref="ApiErrorResponseHelper"/> (respects Dev / Api:ExposeExceptionDetails).</summary>
public static class ControllerServerErrorExtensions
{
    public static ActionResult<ApiResponseDto<T>> ApiServerError<T>(
        this ControllerBase controller,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        Exception? ex = null)
    {
        var (message, errors) = ApiErrorResponseHelper.FormatException(ex, environment, configuration);
        return controller.StatusCode(StatusCodes.Status500InternalServerError,
            new ApiResponseDto<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            });
    }

    /// <summary>For actions that return <see cref="IActionResult"/> (e.g. <c>NoContent()</c>), use this overload — <see cref="ActionResult{T}"/> does not convert to <see cref="IActionResult"/> everywhere.</summary>
    public static IActionResult ApiServerErrorAction<T>(
        this ControllerBase controller,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        Exception? ex = null)
    {
        var (message, errors) = ApiErrorResponseHelper.FormatException(ex, environment, configuration);
        return controller.StatusCode(StatusCodes.Status500InternalServerError,
            new ApiResponseDto<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            });
    }
}
