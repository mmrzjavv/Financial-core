namespace Core.Application.Identity.Common.Interfaces;

public interface IAuditEventLogger
{
    Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}

public sealed record AuditEvent(
    string EventName,
    Guid? UserId,
    Guid? SessionId,
    bool Success,
    string? Reason = null,
    IDictionary<string, object?>? Properties = null
);

