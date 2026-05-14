using BuildingBlocks.Observability.Correlation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;

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
                .Enrich.WithProperty("Application", applicationName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);

            var seqUrl = context.Configuration["Serilog:Seq:ServerUrl"]
                ?? Environment.GetEnvironmentVariable("SEQ_URL");

            if (!string.IsNullOrWhiteSpace(seqUrl))
                configuration.WriteTo.Seq(seqUrl);
        });
    }

    public static void UsePlatformRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);

                if (httpContext.Items.TryGetValue(CorrelationContext.ItemKey, out var correlationId))
                    diagnosticContext.Set("CorrelationId", correlationId);
            };
        });
    }
}
