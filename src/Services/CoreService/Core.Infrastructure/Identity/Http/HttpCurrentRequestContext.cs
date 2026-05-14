using Core.Application.Identity.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Core.Infrastructure.Identity.Http;

public class HttpCurrentRequestContext(IHttpContextAccessor accessor) : ICurrentRequestContext
{
    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => accessor.HttpContext?.Request.Headers.UserAgent.ToString();
    public string? DeviceId => accessor.HttpContext?.Request.Headers["X-Device-Id"].ToString();
    public string? CorrelationId => accessor.HttpContext?.TraceIdentifier;
    public string? TraceId => accessor.HttpContext?.TraceIdentifier;
}
