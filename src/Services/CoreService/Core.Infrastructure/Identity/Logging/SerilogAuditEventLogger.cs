using Core.Application.Identity.Common.Interfaces;

namespace Core.Infrastructure.Identity.Logging;

public class SerilogAuditEventLogger(IStructuredLogger logger) : IAuditEventLogger
{
    public Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        var properties = new Dictionary<string, object>
        {
            { "EventName", auditEvent.EventName },
            { "UserId", auditEvent.UserId?.ToString() ?? string.Empty },
            { "SessionId", auditEvent.SessionId?.ToString() ?? string.Empty },
            { "Success", auditEvent.Success }
        };

        if (!string.IsNullOrEmpty(auditEvent.Reason))
            properties["Reason"] = auditEvent.Reason;

        if (auditEvent.Properties is not null)
        {
            foreach (var kv in auditEvent.Properties)
            {
                properties[kv.Key] = kv.Value ?? string.Empty;
            }
        }

        logger.LogAuditTrail(auditEvent.EventName, auditEvent.UserId?.ToString() ?? "anonymous", properties);
        return Task.CompletedTask;
    }
}
