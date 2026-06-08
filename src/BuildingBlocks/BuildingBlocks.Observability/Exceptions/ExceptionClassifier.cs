using System.Net;
using System.Text.Json;
using BuildingBlocks.Application.Common;
using BuildingBlocks.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Observability.Exceptions;

public static class ExceptionClassifier
{
    public static ClassifiedException Classify(Exception exception)
    {
        var root = Unwrap(exception);

        return root switch
        {
            DomainException domain => new ClassifiedException(
                domain.StatusCode,
                TitleFor(domain.StatusCode),
                domain.Message,
                domain.ErrorCode,
                IsExpected: true,
                LogLevel.Warning),

            DbUpdateConcurrencyException => new ClassifiedException(
                (int)HttpStatusCode.Conflict,
                "Concurrency Conflict",
                SystemMessages.ConcurrencyConflict,
                "concurrency_conflict",
                IsExpected: true,
                LogLevel.Warning),

            DbUpdateException => new ClassifiedException(
                (int)HttpStatusCode.BadRequest,
                "Data Update Failed",
                SystemMessages.DataUpdateFailed,
                "data_update_failed",
                IsExpected: true,
                LogLevel.Warning),

            ArgumentNullException or ArgumentException => new ClassifiedException(
                (int)HttpStatusCode.BadRequest,
                "Bad Request",
                root.Message,
                "validation_error",
                IsExpected: true,
                LogLevel.Warning),

            InvalidOperationException => new ClassifiedException(
                (int)HttpStatusCode.BadRequest,
                "Business Rule Violation",
                root.Message,
                "business_rule_violation",
                IsExpected: true,
                LogLevel.Warning),

            UnauthorizedAccessException => new ClassifiedException(
                (int)HttpStatusCode.Unauthorized,
                "Unauthorized",
                root.Message,
                "unauthorized",
                IsExpected: true,
                LogLevel.Warning),

            KeyNotFoundException => new ClassifiedException(
                (int)HttpStatusCode.NotFound,
                "Not Found",
                root.Message,
                "not_found",
                IsExpected: true,
                LogLevel.Warning),

            JsonException => new ClassifiedException(
                (int)HttpStatusCode.BadRequest,
                "Bad Request",
                SystemMessages.DeserializationReturnedNull,
                "invalid_payload",
                IsExpected: true,
                LogLevel.Warning),

            NotImplementedException => new ClassifiedException(
                (int)HttpStatusCode.NotImplemented,
                "Not Implemented",
                root.Message,
                "not_implemented",
                IsExpected: true,
                LogLevel.Warning),

            _ => new ClassifiedException(
                (int)HttpStatusCode.InternalServerError,
                "Internal Server Error",
                SystemMessages.UnexpectedError,
                "unexpected_error",
                IsExpected: false,
                LogLevel.Error,
                DevelopmentDetail: SanitizeForClient(root.Message))
        };
    }

    private static Exception Unwrap(Exception exception)
    {
        if (exception is AggregateException { InnerExceptions.Count: 1 } aggregate)
            return Unwrap(aggregate.InnerExceptions[0]);

        return exception;
    }

    private static string SanitizeForClient(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return SystemMessages.UnexpectedError;

        if (message.Contains("SELECT ", StringComparison.OrdinalIgnoreCase)
            || message.Contains("INSERT ", StringComparison.OrdinalIgnoreCase)
            || message.Contains("UPDATE ", StringComparison.OrdinalIgnoreCase)
            || message.Contains("DELETE ", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
            || message.Contains("SqlException", StringComparison.OrdinalIgnoreCase))
            return SystemMessages.UnexpectedError;

        return message;
    }

    private static string TitleFor(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        501 => "Not Implemented",
        _ when statusCode >= 500 => "Internal Server Error",
        _ => "Error"
    };
}
