using Microsoft.Extensions.Logging;


namespace Core.Application.Identity.Common.Interfaces;
public interface ILoggingService
{
    void LogUserActivity(string userId, string action, string entity, string? entityId = null, string? details = null);
    void LogSystemOperation(string operation, string? details = null, LogLevel level = LogLevel.Information);
    void LogSecurityEvent(string userId, string action, string? ipAddress = null, string? details = null);
    void LogPerformanceMetric(string operation, TimeSpan duration, string? context = null);
    void LogDataAccess(string userId, string operation, string entity, string? entityId = null, string? details = null);
    void LogValidationFailure(string userId, string entity, string field, string reason);
    void LogBusinessRuleViolation(string userId, string entity, string rule, string? details = null);
}