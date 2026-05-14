using System.Text.Json.Serialization;
using Core.Application.Identity.Events;

namespace Core.Application.Identity.Notifications.Events;

public interface INotificationEvent : IAuthDomainEvent;

public abstract class BaseNotificationEvent : BaseAuthDomainEvent, INotificationEvent
{
    [JsonIgnore]
    public abstract override string EventType { get; }

    protected BaseNotificationEvent() { }

    protected BaseNotificationEvent(Guid? correlationId, Guid? traceId, string? userId, string? sessionId)
        : base(correlationId, traceId, userId, sessionId) { }
}

[JsonDerivedType(typeof(SendOtpNotificationEvent), "SendOtpNotification")]
[JsonDerivedType(typeof(SendSmsNotificationEvent), "SendSmsNotification")]
[JsonDerivedType(typeof(SendBulkSmsNotificationEvent), "SendBulkSmsNotification")]
public abstract class NotificationEvent : BaseNotificationEvent
{
    protected NotificationEvent() { }

    protected NotificationEvent(Guid? correlationId, Guid? traceId, string? userId, string? sessionId)
        : base(correlationId, traceId, userId, sessionId) { }
}

public sealed class SendOtpNotificationEvent : NotificationEvent
{
    public override string EventType => "SendOtpNotification";

    public string MobileNumber { get; }
    public string OtpCode { get; }
    public DateTime ValidTime { get; }

    public SendOtpNotificationEvent(
        string mobileNumber,
        string otpCode,
        DateTime validTime,
        Guid? correlationId = null,
        Guid? traceId = null,
        string? userId = null,
        string? sessionId = null)
        : base(correlationId, traceId, userId, sessionId)
    {
        MobileNumber = mobileNumber;
        OtpCode = otpCode;
        ValidTime = validTime;
    }
}

public sealed class SendSmsNotificationEvent : NotificationEvent
{
    public override string EventType => "SendSmsNotification";

    public string MobileNumber { get; }
    public int MessageId { get; }

    public SendSmsNotificationEvent(
        string mobileNumber,
        int messageId,
        Guid? correlationId = null,
        Guid? traceId = null,
        string? userId = null,
        string? sessionId = null)
        : base(correlationId, traceId, userId, sessionId)
    {
        MobileNumber = mobileNumber;
        MessageId = messageId;
    }
}

public sealed class SendBulkSmsNotificationEvent : NotificationEvent
{
    public override string EventType => "SendBulkSmsNotification";

    public List<string> MobileNumbers { get; }
    public int MessageId { get; }

    public SendBulkSmsNotificationEvent(
        List<string> mobileNumbers,
        int messageId,
        Guid? correlationId = null,
        Guid? traceId = null,
        string? userId = null,
        string? sessionId = null)
        : base(correlationId, traceId, userId, sessionId)
    {
        MobileNumbers = mobileNumbers;
        MessageId = messageId;
    }
}
