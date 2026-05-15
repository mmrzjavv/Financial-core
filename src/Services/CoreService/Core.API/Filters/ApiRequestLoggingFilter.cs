using System.Diagnostics;
using Core.Application.Identity.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Core.API.Filters;

public sealed class ApiRequestLoggingFilter(ILoggingService logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var executed = await next();
        stopwatch.Stop();

        var controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
        var action = context.RouteData.Values["action"]?.ToString() ?? "Unknown";
        logger.LogPerformanceMetric($"{controller}.{action}", stopwatch.Elapsed);

        if (executed.Exception is not null)
            logger.LogSystemOperation($"{controller}.{action}.Failed", executed.Exception.Message);
    }
}
