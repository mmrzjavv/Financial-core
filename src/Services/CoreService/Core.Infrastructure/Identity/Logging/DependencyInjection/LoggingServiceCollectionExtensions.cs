using System;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Core.Application.Identity.Common.Interfaces;

namespace Core.Infrastructure.Identity.Logging.DependencyInjection;

/// <summary>
/// Extension methods for registering logging services in the DI container.
/// </summary>
public static class LoggingServiceCollectionExtensions
{
    /// <summary>
    /// Registers structured logging services with dependency injection.
    /// </summary>
    public static IServiceCollection AddStructuredLogging(
        this IServiceCollection services,
        Serilog.ILogger serilogLogger,
        string environment = "Development",
        string applicationName = "Financial.Core")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(serilogLogger);

        services.AddScoped<ICorrelationIdProvider, HttpContextCorrelationIdProvider>();
        services.AddScoped<IStructuredLogger>(serviceProvider =>
        {
            var correlationIdProvider = serviceProvider.GetRequiredService<ICorrelationIdProvider>();
            return new StructuredLogger(serilogLogger, correlationIdProvider);
        });

        return services;
    }
}
