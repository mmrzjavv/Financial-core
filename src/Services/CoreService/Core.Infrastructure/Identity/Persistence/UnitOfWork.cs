using Core.Application.Identity.Abstractions;
using Services.CoreService.Core.Persistence;

namespace Core.Infrastructure.Identity.Persistence;

public sealed class UnitOfWork(CoreDbContext context) : IUnitOfWork
{
    private IUserRepository? _users;
    private IRefreshTokenRepository? _refreshTokens;
    private IUserSessionRepository? _userSessions;

    public IUserRepository Users => _users ??= new UserRepository(context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(context);
    public IUserSessionRepository UserSessions => _userSessions ??= new UserSessionRepository(context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public void Dispose()
    {
    }
}
