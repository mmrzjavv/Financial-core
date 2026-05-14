using System;
using System.Collections.Generic;

namespace Core.Application.Identity.Common.Interfaces;

/// <summary>
/// Structured logging service for consistent, correlated logging across the application.
/// Provides context-aware logging with built-in correlation and user information.
/// </summary>
public interface IStructuredLogger
{
    /// <summary>
    /// Logs a verbose-level message (very detailed diagnostic info).
    /// </summary>
    void LogVerbose(string message, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs an information-level message (general informational messages).
    /// Use for: API requests received, business operations started/completed, workflow events.
    /// </summary>
    void LogInformation(string message, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs a warning-level message (potentially harmful situations).
    /// Use for: deprecated API usage, slow operations, missing optional data, retry attempts.
    /// </summary>
    void LogWarning(string message, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs an error-level message with exception details.
    /// Use for: caught exceptions, failed operations, resource unavailability.
    /// </summary>
    void LogError(string message, Exception? exception = null, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs a critical-level message indicating system failure.
    /// Use for: unrecoverable errors, database connection failures, fatal exceptions.
    /// </summary>
    void LogCritical(string message, Exception? exception = null, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs an API request with relevant context.
    /// </summary>
    void LogApiRequest(
        string endpoint,
        string method,
        string? userId = null,
        Dictionary<string, object>? additionalProperties = null);

    /// <summary>
    /// Logs an API response with result status.
    /// </summary>
    void LogApiResponse(
        string endpoint,
        string method,
        int statusCode,
        long? durationMs = null,
        Dictionary<string, object>? additionalProperties = null);

    /// <summary>
    /// Logs an authentication event (login, logout, refresh token).
    /// </summary>
    void LogAuthEvent(
        string action,
        string username,
        bool success,
        string? reason = null,
        Dictionary<string, object>? additionalProperties = null);

    /// <summary>
    /// Logs an authorization event (permission check, access denied).
    /// </summary>
    void LogAuthorizationEvent(
        string action,
        string userId,
        string resource,
        bool granted,
        Dictionary<string, object>? additionalProperties = null);

    /// <summary>
    /// Logs an integration event (Kafka published, gRPC called, Redis operation).
    /// </summary>
    void LogIntegrationEvent(
        string integrationType,
        string eventName,
        bool success,
        string? targetService = null,
        long? durationMs = null,
        Dictionary<string, object>? additionalProperties = null);

    /// <summary>
    /// Logs a slow operation warning.
    /// </summary>
    void LogSlowOperation(
        string operationName,
        long durationMs,
        long thresholdMs,
        Dictionary<string, object>? additionalProperties = null);

    /// <summary>
    /// Logs an operation with performance metrics.
    /// </summary>
    void LogOperationWithMetrics(
        string operationName,
        long durationMs,
        bool success,
        Dictionary<string, object>? additionalProperties = null);

    /// <summary>
    /// Logs an audit trail (who did what, when).
    /// </summary>
    void LogAuditTrail(
        string action,
        string userId,
        Dictionary<string, object>? additionalProperties = null);
}
