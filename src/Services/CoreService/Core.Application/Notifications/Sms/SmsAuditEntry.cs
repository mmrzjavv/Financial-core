namespace Core.Application.Notifications.Sms;

public sealed class SmsAuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public SmsTemplateId TemplateId { get; init; }
    public string Mobile { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? Error { get; init; }
    public Guid? CaseId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
