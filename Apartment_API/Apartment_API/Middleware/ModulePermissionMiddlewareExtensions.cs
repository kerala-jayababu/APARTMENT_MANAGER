namespace Apartment_API.Middleware;

public static class ModulePermissionMiddlewareExtensions
{
    public static IApplicationBuilder UseModulePermission(this IApplicationBuilder app) =>
        app.UseMiddleware<ModulePermissionMiddleware>();
}
