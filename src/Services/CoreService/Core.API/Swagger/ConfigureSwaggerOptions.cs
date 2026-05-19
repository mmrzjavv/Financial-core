using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Core.API.Swagger;

internal sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
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
                    Title = OpenApiMetadata.Title,
                    Version = description.ApiVersion.ToString(),
                    Description = OpenApiMetadata.Description,
                    Contact = new OpenApiContact
                    {
                        Name = OpenApiMetadata.ContactName,
                        Email = OpenApiMetadata.ContactEmail,
                        Url = new Uri($"mailto:{OpenApiMetadata.ContactEmail}")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Proprietary — Financial Fund Operations Platform"
                    }
                });
        }
    }
}
