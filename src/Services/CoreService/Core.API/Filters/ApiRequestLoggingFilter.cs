using System.Diagnostics;
using Core.Application.Logging;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Core.API.Filters;

public sealed class ApiRequestLoggingFilter(ILogger<ApiRequestLoggingFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
        var action = context.RouteData.Values["action"]?.ToString() ?? "Unknown";
        var operation = $"{controller}.{action}";

        ApplicationLog.Started(logger, $"HTTP {operation}");

        var executed = await next();
        stopwatch.Stop();

        if (executed.Exception is not null)
        {
            logger.LogError(
                executed.Exception,
                "HTTP {Operation} failed after {ElapsedMs} ms",
                operation,
                stopwatch.ElapsedMilliseconds);
            return;
        }

        ApplicationLog.Completed(
            logger,
            "HTTP {Operation} completed in {ElapsedMs} ms — status {StatusCode}",
            operation,
            stopwatch.ElapsedMilliseconds,
            context.HttpContext.Response.StatusCode);
    }
}
