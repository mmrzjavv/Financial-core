namespace Core.Application.Notifications.Sms;

public interface ISmsAuditStore
{
    Task AppendAsync(SmsAuditEntry entry, CancellationToken cancellationToken = default);
}
