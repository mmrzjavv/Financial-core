using System.Diagnostics;
using BuildingBlocks.Observability.Correlation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Observability.Exceptions;

public sealed class GlobalExceptionHandler(
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var classification = ExceptionClassifier.Classify(exception);
        var traceId = ResolveTraceId(httpContext);

        LogException(httpContext, exception, classification, traceId);

        var problem = new ProblemDetails
        {
            Status = classification.StatusCode,
            Title = classification.Title,
            Detail = classification.GetSafeDetail(environment.IsDevelopment()),
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = traceId;
        if (!string.IsNullOrWhiteSpace(classification.ErrorCode))
            problem.Extensions["errorCode"] = classification.ErrorCode;

        httpContext.Response.StatusCode = classification.StatusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private void LogException(
        HttpContext httpContext,
        Exception exception,
        ClassifiedException classification,
        string traceId)
    {
        var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = traceId,
            ["UserId"] = userId,
            ["RequestPath"] = httpContext.Request.Path.Value,
            ["RequestMethod"] = httpContext.Request.Method,
            ["StatusCode"] = classification.StatusCode,
            ["ErrorCode"] = classification.ErrorCode
        }))
        {
            if (classification.IsExpected)
            {
                logger.Log(
                    classification.LogLevel,
                    exception,
                    "Handled {ExceptionType}: {Detail}",
                    exception.GetType().Name,
                    classification.Detail);
            }
            else
            {
                logger.LogError(
                    exception,
                    "Unhandled {ExceptionType} on {RequestMethod} {RequestPath}",
                    exception.GetType().Name,
                    httpContext.Request.Method,
                    httpContext.Request.Path);
            }
        }
    }

    private static string ResolveTraceId(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(CorrelationContext.ItemKey, out var correlationId)
            && correlationId is string value
            && !string.IsNullOrWhiteSpace(value))
            return value;

        return Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
    }
}
