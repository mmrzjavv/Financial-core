using System;
using System.Collections.Generic;
using Core.Application.Identity.Common.Interfaces;
using Serilog;

namespace Core.Infrastructure.Identity.Logging;

public sealed class StructuredLogger : IStructuredLogger
{
    private readonly Serilog.ILogger _logger;
    private readonly ICorrelationIdProvider _correlationIdProvider;

    public StructuredLogger(
        Serilog.ILogger logger,
        ICorrelationIdProvider correlationIdProvider)
    {
        _logger = logger;
        _correlationIdProvider = correlationIdProvider;
    }

    private static Dictionary<string, object> MergeProperties(
        Dictionary<string, object>? additionalProperties,
        params (string Key, object Value)[] properties)
    {
        var merged = additionalProperties != null
            ? new Dictionary<string, object>(additionalProperties)
            : new Dictionary<string, object>();

        foreach (var (key, value) in properties)
            merged[key] = value;

        return merged;
    }

    public void LogVerbose(string message, Dictionary<string, object>? properties = null)
    {
        var logger = _logger;
        if (properties != null)
        {
            foreach (var prop in properties)
                logger = logger.ForContext(prop.Key, prop.Value);
        }
        logger.Verbose(message);
    }

    public void LogInformation(string message, Dictionary<string, object>? properties = null)
    {
        var logger = _logger;
        if (properties != null)
        {
            foreach (var prop in properties)
                logger = logger.ForContext(prop.Key, prop.Value);
        }
        logger.Information(message);
    }

    public void LogWarning(string message, Dictionary<string, object>? properties = null)
    {
        var logger = _logger;
        if (properties != null)
        {
            foreach (var prop in properties)
                logger = logger.ForContext(prop.Key, prop.Value);
        }
        logger.Warning(message);
    }

    public void LogError(string message, Exception? exception = null, Dictionary<string, object>? properties = null)
    {
        var logger = _logger;
        if (properties != null)
        {
            foreach (var prop in properties)
                logger = logger.ForContext(prop.Key, prop.Value);
        }

        if (exception != null)
            logger.Error(exception, message);
        else
            logger.Error(message);
    }

    public void LogCritical(string message, Exception? exception = null, Dictionary<string, object>? properties = null)
    {
        var logger = _logger;
        if (properties != null)
        {
            foreach (var prop in properties)
                logger = logger.ForContext(prop.Key, prop.Value);
        }

        if (exception != null)
            logger.Fatal(exception, message);
        else
            logger.Fatal(message);
    }

    public void LogApiRequest(
        string endpoint,
        string method,
        string? userId = null,
        Dictionary<string, object>? additionalProperties = null)
    {
        var props = MergeProperties(additionalProperties,
            ("Endpoint", endpoint),
            ("Method", method),
            ("EventType", "ApiRequest"));
        if (!string.IsNullOrEmpty(userId))
            props["UserId"] = userId;

        LogInformation("API request received", props);
    }

    public void LogApiResponse(
        string endpoint,
        string method,
        int statusCode,
        long? durationMs = null,
        Dictionary<string, object>? additionalProperties = null)
    {
        var props = MergeProperties(additionalProperties,
            ("Endpoint", endpoint),
            ("Method", method),
            ("StatusCode", statusCode),
            ("EventType", "ApiResponse"));
        if (durationMs.HasValue)
            props["DurationMs"] = durationMs.Value;

        var message = $"API response: {statusCode}";
        if (statusCode >= 200 && statusCode < 300)
            LogInformation(message, props);
        else if (statusCode >= 400 && statusCode < 500)
            LogWarning(message, props);
        else
            LogError(message, properties: props);
    }

    public void LogAuthEvent(
        string action,
        string username,
        bool success,
        string? reason = null,
        Dictionary<string, object>? additionalProperties = null)
    {
        var props = MergeProperties(additionalProperties,
            ("Action", action),
            ("Username", username),
            ("Success", success),
            ("EventType", "AuthenticationEvent"));
        if (!string.IsNullOrEmpty(reason))
            props["Reason"] = reason;

        var message = success
            ? $"Authentication successful: {action} for {username}"
            : $"Authentication failed: {action} for {username}";

        if (success)
            LogInformation(message, props);
        else
            LogWarning(message, props);
    }

    public void LogAuthorizationEvent(
        string action,
        string userId,
        string resource,
        bool granted,
        Dictionary<string, object>? additionalProperties = null)
    {
        var props = MergeProperties(additionalProperties,
            ("Action", action),
            ("UserId", userId),
            ("Resource", resource),
            ("Granted", granted),
            ("EventType", "AuthorizationEvent"));

        var message = granted
            ? $"Authorization granted: {action} on {resource}"
            : $"Authorization denied: {action} on {resource}";

        if (granted)
            LogInformation(message, props);
        else
            LogWarning(message, props);
    }

    public void LogIntegrationEvent(
        string integrationType,
        string eventName,
        bool success,
        string? targetService = null,
        long? durationMs = null,
        Dictionary<string, object>? additionalProperties = null)
    {
        var props = MergeProperties(additionalProperties,
            ("IntegrationType", integrationType),
            ("EventName", eventName),
            ("Success", success),
            ("EventType", "IntegrationEvent"));
        if (!string.IsNullOrEmpty(targetService))
            props["TargetService"] = targetService;
        if (durationMs.HasValue)
            props["DurationMs"] = durationMs.Value;

        var message = success
            ? $"Integration event succeeded: {eventName}"
            : $"Integration event failed: {eventName}";

        if (success)
            LogInformation(message, props);
        else
            LogError(message, properties: props);
    }

    public void LogSlowOperation(
        string operationName,
        long durationMs,
        long thresholdMs,
        Dictionary<string, object>? additionalProperties = null)
    {
        var props = MergeProperties(additionalProperties,
            ("OperationName", operationName),
            ("DurationMs", durationMs),
            ("ThresholdMs", thresholdMs),
            ("EventType", "SlowOperation"));

        LogWarning($"Slow operation detected: {operationName} took {durationMs}ms (threshold: {thresholdMs}ms)", props);
    }

    public void LogOperationWithMetrics(
        string operationName,
        long durationMs,
        bool success,
        Dictionary<string, object>? additionalProperties = null)
    {
        var props = MergeProperties(additionalProperties,
            ("OperationName", operationName),
            ("DurationMs", durationMs),
            ("Success", success),
            ("EventType", "OperationMetrics"));

        LogInformation($"Operation completed: {operationName} in {durationMs}ms", props);
    }

    public void LogAuditTrail(
        string action,
        string userId,
        Dictionary<string, object>? additionalProperties = null)
    {
        var props = MergeProperties(additionalProperties,
            ("Action", action),
            ("UserId", userId),
            ("EventType", "AuditTrail"));

        LogInformation($"Audit: {action} by user {userId}", props);
    }
}
