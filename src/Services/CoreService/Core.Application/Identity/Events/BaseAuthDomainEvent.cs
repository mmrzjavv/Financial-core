using System.Text.Json.Serialization;

namespace Core.Application.Identity.Events;

public interface IAuthDomainEvent
{
    string EventType { get; }
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    Guid? CorrelationId { get; }
    Guid? TraceId { get; }
    string? UserId { get; }
    string? SessionId { get; }
}

public abstract class BaseAuthDomainEvent : IAuthDomainEvent
{
    [JsonIgnore]
    public abstract string EventType { get; }

    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; set; }
    public Guid? TraceId { get; set; }
    public string? UserId { get; set; }
    public string? SessionId { get; set; }

    protected BaseAuthDomainEvent() { }

    protected BaseAuthDomainEvent(Guid? correlationId, Guid? traceId, string? userId, string? sessionId)
    {
        CorrelationId = correlationId ?? GetCurrentCorrelationId();
        TraceId = traceId ?? GetCurrentTraceId();
        UserId = userId;
        SessionId = sessionId;
    }

    private static Guid? GetCurrentCorrelationId()
    {
        var activity = System.Diagnostics.Activity.Current;
        var correlationIdString = activity?.GetTagItem("correlation.id") as string;

        return correlationIdString != null && Guid.TryParse(correlationIdString, out var guid) ? guid : null;
    }

    private static Guid? GetCurrentTraceId()
    {
        var activity = System.Diagnostics.Activity.Current;
        var traceIdString = activity?.GetTagItem("trace.id") as string ?? activity?.TraceId.ToString();

        return traceIdString != null && Guid.TryParse(traceIdString, out var guid) ? guid : null;
    }
}