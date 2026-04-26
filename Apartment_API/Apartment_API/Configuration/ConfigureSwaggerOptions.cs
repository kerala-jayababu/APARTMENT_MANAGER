using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Apartment_API.Configuration;

public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                new OpenApiInfo
                {
                    Title = "Apartment API",
                    Version = description.ApiVersion.ToString()
                });
        }

        options.DocInclusionPredicate((documentName, apiDescription) =>
            string.Equals(documentName, apiDescription.GroupName, StringComparison.OrdinalIgnoreCase));
    }
}
