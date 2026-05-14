namespace Core.Infrastructure.Identity.Logging;

/// <summary>
/// Provides access to the current correlation ID across different execution contexts.
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// Gets the current correlation ID.
    /// </summary>
    string GetCorrelationId();

    /// <summary>
    /// Sets the correlation ID for the current context.
    /// </summary>
    void SetCorrelationId(string correlationId);
}
