using Core.Application.Abstractions;
using Core.Application.Identity.Abstractions;
using Core.Application.Identity.Common.Interfaces;
using Core.Application.Identity.Common.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Infrastructure.Identity.Http;

/// <summary>
/// Keeps <see cref="Core.Domain.Identity.Entities.UserSession.LastActivityAt"/> fresh for authenticated requests
/// so online-user queries reflect real activity (not only login/token-refresh events).
/// </summary>
public sealed class SessionActivityMiddleware(
    RequestDelegate next,
    ILogger<SessionActivityMiddleware> logger)
{
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserAccessor currentUser,
        IUserSessionRepository sessions,
        ISessionCacheService sessionCache,
        IOptions<AuthSessionOptions> sessionOptions)
    {
        var userId = currentUser.UserId;
        var sessionId = currentUser.SessionId;

        if (userId is not null && sessionId is not null)
        {
            try
            {
                var options = sessionOptions.Value;
                var touchIntervalMinutes = Math.Max(1, options.ActivityTouchIntervalMinutes);
                var staleBeforeUtc = DateTime.UtcNow.AddMinutes(-touchIntervalMinutes);

                var touched = await sessions.TouchLastActivityAsync(sessionId.Value, staleBeforeUtc, context.RequestAborted);
                if (touched > 0)
                    await sessionCache.UpdateLastActivityAsync(sessionId.Value, context.RequestAborted);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to touch session activity for {SessionId}", sessionId);
            }
        }

        await next(context);
    }
}
