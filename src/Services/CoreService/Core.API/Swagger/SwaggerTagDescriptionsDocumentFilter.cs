using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Core.API.Swagger;

/// <summary>Enriches OpenAPI tag descriptions shown in Swagger UI grouping.</summary>
internal sealed class SwaggerTagDescriptionsDocumentFilter : IDocumentFilter
{
    private static readonly Dictionary<string, string> TagDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["InvestmentCases"] = "Investment case lifecycle — workflow, documents (presign/confirm), kanban, payments (one platform module)",
        ["User"] = "Panel users — OTP, login, sessions, profile",
        ["Companies"] = "Applicant company profiles",
        ["Dashboard"] = "Executive dashboards — CEO and board"
    };

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (swaggerDoc.Tags is null)
            return;

        foreach (var tag in swaggerDoc.Tags)
        {
            if (tag.Name is not null && TagDescriptions.TryGetValue(tag.Name, out var description))
                tag.Description = description;
        }

        foreach (var (name, description) in TagDescriptions)
        {
            if (swaggerDoc.Tags.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
                continue;

            swaggerDoc.Tags.Add(new OpenApiTag { Name = name, Description = description });
        }
    }
}
