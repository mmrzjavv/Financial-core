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
            {
                if (exception is not null)
                    return LogEventLevel.Error;

                var statusCode = httpContext.Response.StatusCode;

                if (statusCode >= 500)
                    return LogEventLevel.Error;

                if (statusCode >= 400)
                    return LogEventLevel.Warning;

                // Successful 2xx/3xx and OPTIONS preflights — below Information threshold.
                return LogEventLevel.Verbose;
            };

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
