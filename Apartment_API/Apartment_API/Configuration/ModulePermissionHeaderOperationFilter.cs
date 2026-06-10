using Apartment_API.Middleware;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Apartment_API.Configuration;

/// <summary>Documents X-Module-Code on API routes (except auth).</summary>
public sealed class ModulePermissionHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? string.Empty;
        if (path.Contains("auth", StringComparison.OrdinalIgnoreCase)
            || path.Contains("role-permissions", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/modules", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/module-groups", StringComparison.OrdinalIgnoreCase))
            return;

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = ModulePermissionMiddleware.ModuleCodeHeader,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Module code for RBAC (e.g. M02, M04, M10). Required for non–super-admin API calls.",
            Schema = new OpenApiSchema { Type = "string" }
        });
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = ModulePermissionMiddleware.PermissionActionHeader,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Optional override: View, Create, Edit, Delete, Approve. Inferred from HTTP method when omitted.",
            Schema = new OpenApiSchema { Type = "string" }
        });
    }
}
