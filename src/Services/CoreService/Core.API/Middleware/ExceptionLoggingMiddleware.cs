// using System;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Diagnostics;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
//
// namespace BuildingBlocks.Observability.Logging.Middleware;
//
// /// <summary>
// /// Global exception logging middleware that captures and logs all unhandled exceptions.
// /// Logs include: correlation ID, user info, endpoint, stack trace, and inner exceptions.
// /// </summary>
// public sealed class ExceptionLoggingMiddleware
// {
//     private readonly IStructuredLogger _logger;
//     private readonly ICorrelationIdProvider _correlationIdProvider;
//
//     public ExceptionLoggingMiddleware(
//         IStructuredLogger logger,
//         ICorrelationIdProvider correlationIdProvider)
//     {
//         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//         _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
//     }
//
//     public async Task InvokeAsync(HttpContext context, IExceptionHandlerPathFeature? exceptionFeature)
//     {
//         if (exceptionFeature?.Error is Exception exception)
//         {
//             var properties = new Dictionary<string, object>
//             {
//                 { "ExceptionType", exception.GetType().FullName ?? exception.GetType().Name },
//                 { "Message", exception.Message },
//                 { "Endpoint", context.Request.Path.ToString() },
//                 { "Method", context.Request.Method },
//                 { "StackTrace", exception.StackTrace ?? "No stack trace available" },
//                 { "EventType", "UnhandledException" }
//             };
//
//             // Add inner exception info if available
//             if (exception.InnerException != null)
//             {
//                 properties["InnerException"] = exception.InnerException.GetType().Name;
//                 properties["InnerExceptionMessage"] = exception.InnerException.Message;
//             }
//
//             // Extract user info
//             if (context.User.Identity?.IsAuthenticated == true)
//             {
//                 var userId = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst("user_id")?.Value;
//                 if (!string.IsNullOrEmpty(userId))
//                     properties["UserId"] = userId;
//             }
//
//             // Extract case ID if available
//             if (context.GetRouteValue("id") is string caseIdStr && Guid.TryParse(caseIdStr, out var caseId))
//                 properties["CaseId"] = caseId;
//
//             // Log the exception
//             _logger.LogError(
//                 $"Unhandled exception in {context.Request.Method} {context.Request.Path}",
//                 exception,
//                 properties);
//         }
//
//         await Task.CompletedTask;
//     }
// }
//
// /// <summary>
// /// Exception logging middleware that wraps the standard exception handler.
// /// Use with app.UseExceptionHandler() to automatically log exceptions.
// /// </summary>
// public static class ExceptionLoggingMiddlewareExtensions
// {
//     /// <summary>
//     /// Adds global exception handling with structured logging.
//     /// </summary>
//     public static IApplicationBuilder UseExceptionLogging(
//         this IApplicationBuilder app,
//         IStructuredLogger logger,
//         ICorrelationIdProvider correlationIdProvider)
//     {
//         if (app == null)
//             throw new ArgumentNullException(nameof(app));
//
//         app.UseExceptionHandler(errorApp =>
//         {
//             errorApp.Run(async context =>
//             {
//                 var exceptionHandlerPathFeature =
//                     context.Features.Get<IExceptionHandlerPathFeature>();
//                 var exception = exceptionHandlerPathFeature?.Error;
//
//                 if (exception != null)
//                 {
//                     // Special handling for DbUpdateConcurrencyException
//                     var statusCode = exception.GetType().Name == "DbUpdateConcurrencyException"
//                         ? StatusCodes.Status409Conflict
//                         : StatusCodes.Status500InternalServerError;
//
//                     var properties = new Dictionary<string, object>
//                     {
//                         { "ExceptionType", exception.GetType().FullName ?? exception.GetType().Name },
//                         { "StatusCode", statusCode },
//                         { "EventType", "ExceptionHandled" }
//                     };
//
//                     // Extract case ID
//                     if (context.GetRouteValue("id") is string caseIdStr && Guid.TryParse(caseIdStr, out var caseId))
//                         properties["CaseId"] = caseId;
//
//                     logger.LogError(
//                         $"Exception handled: {exception.GetType().Name}",
//                         exception,
//                         properties);
//
//                     context.Response.StatusCode = statusCode;
//                     context.Response.ContentType = "application/problem+json";
//
//                     var problemDetails = new ProblemDetails
//                     {
//                         Type = $"https://tools.ietf.org/html/rfc7231#section-6.6.1",
//                         Title = "Internal Server Error",
//                         Status = statusCode,
//                         Detail = exception.Message,
//                         Instance = context.Request.Path
//                     };
//
//                     await context.Response.WriteAsJsonAsync(problemDetails);
//                 }
//             });
//         });
//
//         return app;
//     }
// }
