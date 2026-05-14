namespace Core.Application.Identity.Common.Interfaces;

public interface ICurrentRequestContext
{
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? DeviceId { get; }
    string? CorrelationId { get; }
    string? TraceId { get; }
}
