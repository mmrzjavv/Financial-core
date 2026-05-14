namespace Core.Application.Identity.Abstractions;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IUserSessionRepository UserSessions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
