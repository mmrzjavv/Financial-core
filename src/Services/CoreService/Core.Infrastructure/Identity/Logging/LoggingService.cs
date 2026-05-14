using Core.Application.Identity.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Identity.Logging;

public class LoggingService(IStructuredLogger logger) : ILoggingService
{
    public void LogUserActivity(string userId, string action, string entity, string? entityId = null, string? details = null)
    {
        var props = new Dictionary<string, object>
        {
            { "ActionName", action },
            { "Entity", entity },
            { "EventType", "UserActivity" }
        };

        if (!string.IsNullOrEmpty(entityId)) props["EntityId"] = entityId;
        if (!string.IsNullOrEmpty(details)) props["Details"] = details;

        logger.LogAuditTrail(action, userId, props);
    }

    public void LogSystemOperation(string operation, string? details = null, LogLevel level = LogLevel.Information)
    {
        var props = new Dictionary<string, object>
        {
            { "Operation", operation },
            { "EventType", "SystemOperation" }
        };

        if (!string.IsNullOrEmpty(details)) props["Details"] = details;

        var message = $"System Operation: {operation}";

        switch (level)
        {
            case LogLevel.Debug:
                logger.LogVerbose(message, props);
                break;
            case LogLevel.Information:
                logger.LogInformation(message, props);
                break;
            case LogLevel.Warning:
                logger.LogWarning(message, props);
                break;
            case LogLevel.Error:
                logger.LogError(message, properties: props);
                break;
        }
    }

    public void LogSecurityEvent(string userId, string action, string? ipAddress = null, string? details = null)
    {
        var props = new Dictionary<string, object>
        {
            { "Action", action },
            { "EventType", "SecurityEvent" }
        };

        if (!string.IsNullOrEmpty(ipAddress)) props["IpAddress"] = ipAddress;
        if (!string.IsNullOrEmpty(details)) props["Details"] = details;

        logger.LogAuthEvent(action, userId, false, details, props);
    }

    public void LogPerformanceMetric(string operation, TimeSpan duration, string? context = null)
    {
        var props = new Dictionary<string, object>
        {
            { "Context", context ?? "None" }
        };

        logger.LogOperationWithMetrics(operation, (long)duration.TotalMilliseconds, true, props);
    }

    public void LogDataAccess(string userId, string operation, string entity, string? entityId = null, string? details = null)
    {
        var props = new Dictionary<string, object>
        {
            { "Operation", operation },
            { "Entity", entity },
            { "EventType", "DataAccess" }
        };

        if (!string.IsNullOrEmpty(entityId)) props["EntityId"] = entityId;
        if (!string.IsNullOrEmpty(details)) props["Details"] = details;

        logger.LogAuditTrail($"DataAccess.{operation}", userId, props);
    }

    public void LogValidationFailure(string userId, string entity, string field, string reason)
    {
        var props = new Dictionary<string, object>
        {
            { "Entity", entity },
            { "Field", field },
            { "Reason", reason },
            { "EventType", "ValidationFailure" }
        };

        logger.LogWarning($"Validation failure for {entity}.{field}: {reason}", props);
    }

    public void LogBusinessRuleViolation(string userId, string entity, string rule, string? details = null)
    {
        var props = new Dictionary<string, object>
        {
            { "Entity", entity },
            { "Rule", rule },
            { "EventType", "BusinessRuleViolation" }
        };

        if (!string.IsNullOrEmpty(details)) props["Details"] = details;

        logger.LogWarning($"Business rule violation in {entity}: {rule}", props);
    }
}
