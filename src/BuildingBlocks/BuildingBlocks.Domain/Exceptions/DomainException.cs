namespace BuildingBlocks.Domain.Exceptions;

/// <summary>
/// Base type for expected business/domain failures surfaced to callers.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message, int statusCode, string? errorCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public int StatusCode { get; }
    public string? ErrorCode { get; }
}
