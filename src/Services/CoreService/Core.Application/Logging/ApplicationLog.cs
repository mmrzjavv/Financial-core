using Microsoft.Extensions.Logging;

namespace Core.Application.Logging;

/// <summary>
/// Shared structured Information logs for application and infrastructure use cases (not a DI service).
/// Pattern per operation: <see cref="Started"/> → optional <see cref="Blocked"/> on expected denials → <see cref="Completed"/> on success.
/// Controllers stay thin; do not duplicate these messages at the API layer.
/// </summary>
public static class ApplicationLog
{
    public static void Started(ILogger logger, string operation, string? userId = null, Guid? caseId = null)
    {
        if (userId is not null && caseId.HasValue)
        {
            logger.LogInformation(
                "Starting {Operation} — user {UserId}, case {CaseId}",
                operation, userId, caseId.Value);
            return;
        }

        if (userId is not null)
        {
            logger.LogInformation("Starting {Operation} — user {UserId}", operation, userId);
            return;
        }

        logger.LogInformation("Starting {Operation}", operation);
    }

    public static void Blocked(ILogger logger, string operation, string reason, string? userId = null, Guid? caseId = null)
        => logger.LogInformation(
            "{Operation} did not run — {Reason}. User={UserId}, CaseId={CaseId}",
            operation,
            reason,
            userId ?? "n/a",
            caseId?.ToString() ?? "n/a");

    public static void Completed(ILogger logger, string messageTemplate, params object?[] args)
        => logger.LogInformation(messageTemplate, args);
}
