using System;
using System.Threading;

namespace Core.Infrastructure.Identity.Logging;

/// <summary>
/// Implementation of ICorrelationIdProvider using AsyncLocal to track correlation ID across async calls.
/// </summary>
public sealed class AsyncContextCorrelationIdProvider : ICorrelationIdProvider
{
    private static readonly AsyncLocal<string> _correlationId = new();

    public string GetCorrelationId()
    {
        return _correlationId.Value ?? string.Empty;
    }

    public void SetCorrelationId(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be null or empty.", nameof(correlationId));

        _correlationId.Value = correlationId;
    }
}
