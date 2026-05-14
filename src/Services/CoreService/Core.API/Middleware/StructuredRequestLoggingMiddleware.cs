// using System;
// using System.Diagnostics;
// using System.IO;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Http;
//
// namespace BuildingBlocks.Observability.Logging.Middleware;
//
// /// <summary>
// /// Middleware that logs structured information about HTTP requests and responses.
// /// Logs endpoint, method, status code, duration, user info, and case ID (when available).
// /// </summary>
// public sealed class StructuredRequestLoggingMiddleware
// {
//     private readonly RequestDelegate _next;
//     private readonly IStructuredLogger _logger;
//
//     public StructuredRequestLoggingMiddleware(
//         RequestDelegate next,
//         IStructuredLogger logger)
//     {
//         _next = next ?? throw new ArgumentNullException(nameof(next));
//         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//     }
//
//     public async Task InvokeAsync(HttpContext context)
//     {
//         var stopwatch = Stopwatch.StartNew();
//         var originalBodyStream = context.Response.Body;
//
//         try
//         {
//             // Log incoming request
//             LogRequest(context);
//
//             // Capture response to log it later
//             using var memoryStream = new MemoryStream();
//             context.Response.Body = memoryStream;
//
//             await _next(context);
//
//             stopwatch.Stop();
//
//             // Log response
//             await LogResponseAsync(context, stopwatch.ElapsedMilliseconds, originalBodyStream, memoryStream);
//         }
//         catch (Exception ex)
//         {
//             stopwatch.Stop();
//             _logger.LogError(
//                 $"Request processing failed: {context.Request.Method} {context.Request.Path}",
//                 ex,
//                 new Dictionary<string, object>
//                 {
//                     { "Endpoint", context.Request.Path.ToString() },
//                     { "Method", context.Request.Method },
//                     { "DurationMs", stopwatch.ElapsedMilliseconds },
//                     { "EventType", "RequestFailed" }
//                 });
//
//             throw;
//         }
//     }
//
//     private void LogRequest(HttpContext context)
//     {
//         var properties = new Dictionary<string, object>
//         {
//             { "Endpoint", context.Request.Path.ToString() },
//             { "Method", context.Request.Method },
//             { "QueryString", context.Request.QueryString.ToString() },
//             { "RemoteIP", context.Connection.RemoteIpAddress?.ToString() ?? "Unknown" }
//         };
//
//         // Extract user info from claims
//         if (context.User.Identity?.IsAuthenticated == true)
//         {
//             var userId = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst("user_id")?.Value;
//             if (!string.IsNullOrEmpty(userId))
//                 properties["UserId"] = userId;
//
//             var role = context.User.FindFirst("role")?.Value;
//             if (!string.IsNullOrEmpty(role))
//                 properties["Role"] = role;
//         }
//
//         // Extract case ID from route or query string if available
//         if (context.GetRouteValue("id") is string caseIdStr && Guid.TryParse(caseIdStr, out var caseId))
//             properties["CaseId"] = caseId;
//         else if (context.Request.Query.TryGetValue("caseId", out var caseIdQuery) && Guid.TryParse(caseIdQuery.ToString(), out var caseIdFromQuery))
//             properties["CaseId"] = caseIdFromQuery;
//
//         _logger.LogApiRequest(
//             context.Request.Path.ToString(),
//             context.Request.Method,
//             additionalProperties: properties);
//     }
//
//     private async Task LogResponseAsync(
//         HttpContext context,
//         long durationMs,
//         Stream originalBodyStream,
//         MemoryStream memoryStream)
//     {
//         // Copy response back to original stream
//         memoryStream.Seek(0, SeekOrigin.Begin);
//         await memoryStream.CopyToAsync(originalBodyStream);
//
//         var properties = new Dictionary<string, object>
//         {
//             { "ContentLength", memoryStream.Length }
//         };
//
//         // Extract case ID from route if available
//         if (context.GetRouteValue("id") is string caseIdStr && Guid.TryParse(caseIdStr, out var caseId))
//             properties["CaseId"] = caseId;
//
//         _logger.LogApiResponse(
//             context.Request.Path.ToString(),
//             context.Request.Method,
//             context.Response.StatusCode,
//             durationMs,
//             properties);
//     }
// }
//
// /// <summary>
// /// Extension methods for registering structured request logging middleware.
// /// </summary>
// public static class StructuredRequestLoggingMiddlewareExtensions
// {
//     public static IApplicationBuilder UseStructuredRequestLogging(
//         this IApplicationBuilder app,
//         IStructuredLogger logger)
//     {
//         if (app == null)
//             throw new ArgumentNullException(nameof(app));
//
//         return app.UseMiddleware<StructuredRequestLoggingMiddleware>(logger);
//     }
// }
