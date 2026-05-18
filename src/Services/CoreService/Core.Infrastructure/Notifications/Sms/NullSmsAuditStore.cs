using Core.Application.Notifications.Sms;

namespace Core.Infrastructure.Notifications.Sms;

public sealed class NullSmsAuditStore : ISmsAuditStore
{
    public Task AppendAsync(SmsAuditEntry entry, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
