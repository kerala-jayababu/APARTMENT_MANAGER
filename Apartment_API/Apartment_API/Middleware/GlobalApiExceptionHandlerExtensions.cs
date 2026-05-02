using Apartment_API.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Apartment_API.Middleware;

public static class GlobalApiExceptionHandlerExtensions
{
    /// <summary>
    /// Returns <see cref="DTO.ApiResponseDto{T}"/> JSON for any unhandled exception.
    /// Detail text is included when <c>Development</c> or <see cref="ApiErrorResponseHelper.ExposeExceptionDetailsKey"/> is true.
    /// </summary>
    public static IApplicationBuilder UseGlobalApiExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var ex = ApiErrorResponseHelper.GetUnhandledException(context);
                var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
                var cfg = context.RequestServices.GetRequiredService<IConfiguration>();
                await ApiErrorResponseHelper.WriteJsonAsync(context, ex, env, cfg).ConfigureAwait(false);
            });
        });
    }
}
