using Apartment_API.DTO;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Apartment_API.Configuration;

/// <summary>Consistent 500 <see cref="ApiResponseDto{T}"/> and whether to include exception text (dev / config).</summary>
public static class ApiErrorResponseHelper
{
    public const string ExposeExceptionDetailsKey = "Api:ExposeExceptionDetails";

    public static bool ShouldExposeDetails(IWebHostEnvironment env, IConfiguration config) =>
        config.GetValue(ExposeExceptionDetailsKey, defaultValue: true);

    public static (string Message, IReadOnlyList<string> Errors) FormatException(
        Exception? ex, IWebHostEnvironment env, IConfiguration config)
    {
        if (ex is null)
            return ("An unexpected error occurred.", ["INTERNAL_SERVER_ERROR"]);

        if (FindSqlException(ex) is { } sqlEx)
        {
            var msg = sqlEx.Message;
            return (msg, ["INTERNAL_SERVER_ERROR", $"{sqlEx.GetType().Name}: {msg}"]);
        }

        if (!ShouldExposeDetails(env, config))
            return ("An unexpected error occurred.", ["INTERNAL_SERVER_ERROR"]);
        var inner = ex.InnerException is { } i ? $" Inner: {i.Message}" : "";
        var detail = $"{ex.Message}{inner}";
        return (detail, ["INTERNAL_SERVER_ERROR", $"{ex.GetType().Name}: {detail}"]);
    }

    private static SqlException? FindSqlException(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql) return sql;
        }
        return null;
    }

    public static async Task WriteJsonAsync(HttpContext context, Exception? ex, IWebHostEnvironment env, IConfiguration config)
    {
        var log = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalException");
        if (ex != null)
            log.LogError(ex, "Unhandled exception.");

        var (message, errors) = FormatException(ex, env, config);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ApiResponseDto<object?>
        {
            Success = false,
            Message = message,
            Data = null,
            Errors = errors
        }).ConfigureAwait(false);
    }

    /// <summary>Used by exception-handler middleware to resolve the faulting exception.</summary>
    public static Exception? GetUnhandledException(HttpContext context)
    {
        var feature = context.Features.Get<IExceptionHandlerPathFeature>();
        return feature?.Error;
    }
}
