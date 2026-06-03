using System.Net;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Observability.Correlation;
using Core.Application.Common;
using Core.API.Swagger;
using Microsoft.AspNetCore.Diagnostics;

namespace Core.API.DependencyInjection;

public static class WebApplicationPipelineExtensions
{
    public static WebApplication UseCoreApiPipeline(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var ex = exceptionFeature?.Error;

                if (ex is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException
                    or Microsoft.EntityFrameworkCore.DbUpdateException { InnerException: Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException })
                {
                    var envelope = new ApiOperationResult<object?>().Failed(
                        ApiMessages.ConcurrencyConflict,
                        HttpStatusCode.Conflict,
                        exMessage: app.Environment.IsDevelopment() ? ex.ToString() : null);

                    context.Response.StatusCode = (int)envelope.Status;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(envelope);
                    return;
                }

                var failure = new ApiOperationResult<object?>().Failed(
                    ApiMessages.UnexpectedError,
                    HttpStatusCode.InternalServerError,
                    exMessage: app.Environment.IsDevelopment() ? ex?.Message : null);

                context.Response.StatusCode = (int)failure.Status;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(failure);
            });
        });

        app.UseMiddleware<CorrelationIdMiddleware>();

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
