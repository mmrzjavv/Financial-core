using BuildingBlocks.Application.Common;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Observability.Exceptions;

public sealed record ClassifiedException(
    int StatusCode,
    string Title,
    string Detail,
    string? ErrorCode,
    bool IsExpected,
    LogLevel LogLevel,
    string? DevelopmentDetail = null)
{
    public string GetSafeDetail(bool isDevelopment)
    {
        if (IsExpected)
            return Detail;

        if (isDevelopment && !string.IsNullOrWhiteSpace(DevelopmentDetail))
            return DevelopmentDetail;

        return StatusCode >= 500 ? SystemMessages.UnexpectedError : Detail;
    }
}
