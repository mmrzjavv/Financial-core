using System;
using Serilog.Core;
using Serilog.Events;

namespace Core.Infrastructure.Identity.Logging;

/// <summary>
/// Serilog enricher that automatically adds correlation ID to all logs.
/// </summary>
public sealed class CorrelationIdEnricher : ILogEventEnricher
{
    private readonly ICorrelationIdProvider _correlationIdProvider;

    public CorrelationIdEnricher(ICorrelationIdProvider correlationIdProvider)
    {
        _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var correlationId = _correlationIdProvider.GetCorrelationId();
        if (!string.IsNullOrEmpty(correlationId))
        {
            var property = propertyFactory.CreateProperty("CorrelationId", correlationId);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}

/// <summary>
/// Serilog enricher that automatically adds environment information to all logs.
/// </summary>
public sealed class EnvironmentEnricher : ILogEventEnricher
{
    private readonly string _environment;
    private readonly string _machineName;
    private readonly string _applicationName;

    public EnvironmentEnricher(string environment, string applicationName = "IdentityService")
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _machineName = Environment.MachineName;
        _applicationName = applicationName;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Environment", _environment));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("MachineName", _machineName));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Application", _applicationName));
    }
}

/// <summary>
/// Serilog enricher that automatically adds thread information to logs.
/// </summary>
public sealed class ThreadInfoEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ThreadId", System.Threading.Thread.CurrentThread.ManagedThreadId));
    }
}
