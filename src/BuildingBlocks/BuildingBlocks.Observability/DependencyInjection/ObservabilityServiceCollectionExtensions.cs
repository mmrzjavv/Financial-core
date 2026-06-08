using BuildingBlocks.Observability.Exceptions;
using BuildingBlocks.Observability.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Observability.DependencyInjection;

public static class ObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformObservability(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            };
        });

        services.AddSingleton<HttpContextLogEnrichmentMiddleware>();
        return services;
    }

    public static WebApplication UsePlatformObservabilityPipeline(this WebApplication app)
    {
        app.UseMiddleware<HttpContextLogEnrichmentMiddleware>();
        app.UsePlatformRequestLogging();
        return app;
    }
}
