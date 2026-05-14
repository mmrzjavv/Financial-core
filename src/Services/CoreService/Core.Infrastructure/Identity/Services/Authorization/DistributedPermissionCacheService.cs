using Core.Application.Identity.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Core.Infrastructure.Identity.Services.Authorization;

public class DistributedPermissionCacheService : IPermissionCacheService
{
    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    public DistributedPermissionCacheService(IDistributedCache cache)
    {
        _cache = cache;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<IEnumerable<string>?> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(userId);
        var cachedValue = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (string.IsNullOrEmpty(cachedValue))
        {
            return null;
        }

        return JsonSerializer.Deserialize<IEnumerable<string>>(cachedValue, _jsonOptions);
    }

    public async Task SetUserPermissionsAsync(Guid userId, IEnumerable<string> permissions, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(userId);
        var serializedPermissions = JsonSerializer.Serialize(permissions, _jsonOptions);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(30)
        };

        await _cache.SetStringAsync(cacheKey, serializedPermissions, options, cancellationToken);
    }

    public async Task RemoveUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(userId);
        await _cache.RemoveAsync(cacheKey, cancellationToken);
    }

    private static string GetCacheKey(Guid userId) => $"permissions:user:{userId}";
}