using System;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Core.Application.Identity.Common.Interfaces;

namespace Core.Infrastructure.Identity.Logging.DependencyInjection;

/// <summary>
/// Extension methods for registering logging services in the DI container.
/// Standardized to match CoreService.
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
        string applicationName = "IdentityService")
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (serilogLogger == null)
            throw new ArgumentNullException(nameof(serilogLogger));

        // Register correlation ID provider as singleton
        services.AddSingleton<ICorrelationIdProvider, AsyncContextCorrelationIdProvider>();

        // Register structured logger
        services.AddScoped<IStructuredLogger>(serviceProvider =>
        {
            var correlationIdProvider = serviceProvider.GetRequiredService<ICorrelationIdProvider>();
            return new StructuredLogger(serilogLogger, correlationIdProvider);
        });

        // Configure Serilog enrichers
      

        return services;
    }
}
