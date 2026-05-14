using BuildingBlocks.Observability.Correlation;
using Core.Infrastructure.Common;
using Microsoft.AspNetCore.Http;

namespace Core.Infrastructure.Identity.Logging;

public sealed class HttpContextCorrelationIdProvider(IHttpContextAccessor httpContextAccessor) : ICorrelationIdProvider
{
    public string GetCorrelationId()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue(CorrelationContext.ItemKey, out var correlationId) == true
            && correlationId is string id
            && !string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        return httpContext?.TraceIdentifier ?? string.Empty;
    }

    public void SetCorrelationId(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException(InfrastructureMessages.CorrelationIdRequired, nameof(correlationId));

        if (httpContextAccessor.HttpContext is null)
            return;

        httpContextAccessor.HttpContext.Items[CorrelationContext.ItemKey] = correlationId;
    }
}
