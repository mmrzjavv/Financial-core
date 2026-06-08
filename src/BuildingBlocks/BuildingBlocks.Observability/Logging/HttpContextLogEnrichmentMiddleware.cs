using System.Security.Claims;
using BuildingBlocks.Observability.Correlation;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace BuildingBlocks.Observability.Logging;

public sealed class HttpContextLogEnrichmentMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var correlationId = context.Items.TryGetValue(CorrelationContext.ItemKey, out var value)
            ? value?.ToString()
            : context.TraceIdentifier;

        using (LogContext.PushProperty("TraceId", correlationId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        {
            await next(context);
        }
    }
}
