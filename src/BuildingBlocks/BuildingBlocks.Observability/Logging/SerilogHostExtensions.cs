using BuildingBlocks.Observability.Correlation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

namespace BuildingBlocks.Observability.Logging;

public static class SerilogHostExtensions
{
    public const string DefaultApplicationName = "Financial.Core";

    public static void UsePlatformSerilog(this IHostBuilder hostBuilder, string applicationName = DefaultApplicationName)
    {
        hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.WithExceptionDetails()
                .Enrich.WithProperty("Application", applicationName);

            var seqUrl = context.Configuration["Serilog:Seq:ServerUrl"]
                ?? Environment.GetEnvironmentVariable("SEQ_URL");

            if (!string.IsNullOrWhiteSpace(seqUrl))
            {
                configuration.WriteTo.Async(
                    sink => sink.Seq(seqUrl),
                    blockWhenFull: false,
                    bufferSize: 50_000);
            }
        });
    }

    public static void UsePlatformRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, _, exception) =>
                exception is not null
                    ? LogEventLevel.Error
                    : httpContext.Response.StatusCode >= 500
                        ? LogEventLevel.Error
                        : httpContext.Response.StatusCode >= 400
                            ? LogEventLevel.Warning
                            : LogEventLevel.Information;

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);

                if (httpContext.Items.TryGetValue(CorrelationContext.ItemKey, out var correlationId))
                    diagnosticContext.Set("CorrelationId", correlationId);

                var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId))
                    diagnosticContext.Set("UserId", userId);
            };
        });
    }
}
