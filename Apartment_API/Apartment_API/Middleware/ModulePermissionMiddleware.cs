using System.Text.Json;
using Apartment_API.Configuration;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Apartment_API.Middleware;

public sealed class ModulePermissionMiddleware(RequestDelegate next)
{
    public const string ModuleCodeHeader = "X-Module-Code";
    public const string PermissionActionHeader = "X-Permission-Action";

    public async Task InvokeAsync(HttpContext context, IModulePermissionService permissions, ICurrentUser currentUser)
    {
        if (!ShouldEnforce(context, currentUser))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (!TryGetModuleCode(context, out var moduleCode, out var moduleError))
        {
            await WriteJsonAsync(context, StatusCodes.Status400BadRequest, moduleError!, ["MODULE_CODE_REQUIRED"]).ConfigureAwait(false);
            return;
        }

        if (!ModuleCodes.IsKnown(moduleCode))
        {
            await WriteJsonAsync(
                context,
                StatusCodes.Status400BadRequest,
                $"Unknown module code '{moduleCode}'.",
                ["INVALID_MODULE_CODE"]).ConfigureAwait(false);
            return;
        }

        if (currentUser.IdApartment is not { } apartmentId || currentUser.ApartmentUserRoleId is not { } roleId)
        {
            await WriteJsonAsync(
                context,
                StatusCodes.Status403Forbidden,
                "Apartment context is required. Use a tenant access token with apartment_id.",
                ["NO_APARTMENT_CONTEXT"]).ConfigureAwait(false);
            return;
        }

        var action = ResolvePermissionAction(context);
        try
        {
            await permissions.EnsureAllowedAsync(apartmentId, roleId, moduleCode, action, context.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (ModulePermissionDeniedException ex)
        {
            await WriteJsonAsync(context, StatusCodes.Status403Forbidden, ex.Message, ["MODULE_PERMISSION_DENIED"])
                .ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool ShouldEnforce(HttpContext context, ICurrentUser currentUser)
    {
        if (currentUser is not { IsAuthenticated: true })
            return false;
        if (currentUser.IsSuperAdmin)
            return false;

        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            return false;
        if (IsAllowlistedPath(path))
            return false;

        return true;
    }

    private static bool IsAllowlistedPath(string path)
    {
        if (path.StartsWith("/api/v", StringComparison.OrdinalIgnoreCase)
            && path.Contains("/auth", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.Contains("/role-permissions", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.EndsWith("/modules", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.EndsWith("/module-groups", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    private static bool TryGetModuleCode(HttpContext context, out string moduleCode, out string? error)
    {
        if (!context.Request.Headers.TryGetValue(ModuleCodeHeader, out var values)
            || string.IsNullOrWhiteSpace(values.ToString()))
        {
            moduleCode = string.Empty;
            error = $"{ModuleCodeHeader} header is required (e.g. M02 for Unit management).";
            return false;
        }

        moduleCode = values.ToString().Trim();
        error = null;
        return true;
    }

    private static PermissionAction ResolvePermissionAction(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(PermissionActionHeader, out var actionValues)
            && !string.IsNullOrWhiteSpace(actionValues.ToString())
            && TryParseAction(actionValues.ToString(), out var parsed))
            return parsed;

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.Contains("/approve", StringComparison.OrdinalIgnoreCase))
            return PermissionAction.Approve;

        return context.Request.Method.ToUpperInvariant() switch
        {
            "GET" or "HEAD" => PermissionAction.View,
            "POST" => PermissionAction.Create,
            "PUT" or "PATCH" => PermissionAction.Edit,
            "DELETE" => PermissionAction.Delete,
            _ => PermissionAction.View
        };
    }

    private static bool TryParseAction(string value, out PermissionAction action)
    {
        if (Enum.TryParse(value.Trim(), ignoreCase: true, out action))
            return true;
        action = PermissionAction.View;
        return false;
    }

    private static async Task WriteJsonAsync(HttpContext context, int statusCode, string message, IReadOnlyList<string> errors)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var body = new ApiResponseDto<object?>
        {
            Success = false,
            Message = message,
            Data = null,
            Errors = errors
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(body)).ConfigureAwait(false);
    }
}
