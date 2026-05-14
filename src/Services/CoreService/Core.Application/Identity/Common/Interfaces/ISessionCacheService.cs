namespace Core.Application.Identity.Common.Interfaces;

public interface ISessionCacheService
{
    Task StoreSessionAsync(SessionDescriptor session, CancellationToken cancellationToken);
    Task<bool> ValidateSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken);
    Task UpdateLastActivityAsync(Guid sessionId, CancellationToken cancellationToken);
}

public sealed record SessionDescriptor(
    Guid SessionId,
    Guid UserId,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt
);

