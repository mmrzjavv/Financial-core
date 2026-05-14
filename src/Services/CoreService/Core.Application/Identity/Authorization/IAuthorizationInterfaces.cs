using Services.CoreService.Core.Domain.Identity.Entities;

namespace Core.Application.Identity.Authorization;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(User user, string permission, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetUserPermissionsAsync(User user, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetRolePermissionsAsync(string role, CancellationToken cancellationToken = default);
}

public interface IAuthorizationService
{
    Task<bool> AuthorizeAsync(User user, string permission, CancellationToken cancellationToken = default);
    Task<bool> AuthorizeAsync(Guid userId, string permission, CancellationToken cancellationToken = default);
}

public interface IPermissionCacheService
{
    Task<IEnumerable<string>?> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SetUserPermissionsAsync(Guid userId, IEnumerable<string> permissions, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}