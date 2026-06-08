using BuildingBlocks.Observability.Correlation;
using BuildingBlocks.Observability.DependencyInjection;
using Core.API.Swagger;
using Core.Infrastructure.Identity.Http;

namespace Core.API.DependencyInjection;

public static class WebApplicationPipelineExtensions
{
    public static WebApplication UseCoreApiPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UsePlatformObservabilityPipeline();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = OpenApiMetadata.Title;
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.DefaultModelsExpandDepth(2);

            var descriptions = app.DescribeApiVersions();
            foreach (var description in descriptions)
            {
                var url = $"/swagger/{description.GroupName}/swagger.json";
                options.SwaggerEndpoint(url, $"{OpenApiMetadata.Title} ({description.GroupName})");
            }

            options.RoutePrefix = "swagger";
        });

        app.UseCors("CorsPolicy");
        app.UseAuthentication();
        app.UseMiddleware<SessionActivityMiddleware>();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health");

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        foreach (var address in app.Urls)
        {
            logger.LogInformation("Application is running on: {Address}", address);
            logger.LogInformation("Swagger UI available at: {Address}/swagger", address);
        }

        return app;
    }
}
