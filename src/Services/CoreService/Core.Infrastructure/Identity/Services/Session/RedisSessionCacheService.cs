using System.Text.Json;
using Core.Application.Identity.Common.Interfaces;
using Core.Application.Identity.Common.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Core.Infrastructure.Identity.Services.Session;

public class RedisSessionCacheService : ISessionCacheService
{
    private readonly AuthSessionOptions _options;
    private readonly string? _redisConnectionString;
    private readonly Lazy<Task<ConnectionMultiplexer?>> _lazyConnection;

    public RedisSessionCacheService(IConfiguration configuration, IOptions<AuthSessionOptions> options)
    {
        _options = options.Value;
        _redisConnectionString = configuration.GetConnectionString("Redis");

        _lazyConnection = new Lazy<Task<ConnectionMultiplexer?>>(async () =>
        {
            if (!_options.RedisEnabled || string.IsNullOrWhiteSpace(_redisConnectionString))
                return null;

            try
            {
                return await ConnectionMultiplexer.ConnectAsync(_redisConnectionString);
            }
            catch
            {
                return null;
            }
        });
    }

    public async Task StoreSessionAsync(SessionDescriptor session, CancellationToken cancellationToken)
    {
        var db = await GetDbAsync();
        if (db is null)
            return;

        var key = GetSessionKey(session.SessionId);
        var ttl = session.ExpiresAt - DateTimeOffset.UtcNow;
        if (ttl <= TimeSpan.Zero)
            ttl = TimeSpan.FromDays(Math.Max(1, _options.AbsoluteExpirationDays));

        await db.StringSetAsync(key, JsonSerializer.Serialize(session), ttl);

        // Track per-user sessions for "revoke all"
        var userIndexKey = GetUserSessionsKey(session.UserId);
        await db.SetAddAsync(userIndexKey, session.SessionId.ToString());
        await db.KeyExpireAsync(userIndexKey, TimeSpan.FromDays(Math.Max(1, _options.AbsoluteExpirationDays)));
    }

    public async Task<bool> ValidateSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var db = await GetDbAsync();
        if (db is null)
            return true; // degrade gracefully

        var key = GetSessionKey(sessionId);
        var json = await db.StringGetAsync(key);
        if (!json.HasValue)
            return false;

        SessionDescriptor? session;
        try
        {
            session = JsonSerializer.Deserialize<SessionDescriptor>(json.ToString()!);
        }
        catch
        {
            return false;
        }

        if (session is null || session.UserId != userId)
            return false;

        if (_options.SlidingExpirationMinutes > 0)
        {
            var ttl = TimeSpan.FromMinutes(_options.SlidingExpirationMinutes);
            await db.KeyExpireAsync(key, ttl);
        }

        return true;
    }

    public async Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var db = await GetDbAsync();
        if (db is null)
            return;

        var key = GetSessionKey(sessionId);
        var json = await db.StringGetAsync(key);
        if (json.HasValue)
        {
            try
            {
                var session = JsonSerializer.Deserialize<SessionDescriptor>(json.ToString()!);
                if (session is not null)
                {
                    await db.SetRemoveAsync(GetUserSessionsKey(session.UserId), sessionId.ToString());
                }
            }
            catch
            {
                // ignore
            }
        }

        await db.KeyDeleteAsync(key);
    }

    public async Task RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var db = await GetDbAsync();
        if (db is null)
            return;

        var indexKey = GetUserSessionsKey(userId);
        var sessionIds = await db.SetMembersAsync(indexKey);
        foreach (var sessionId in sessionIds)
        {
            if (Guid.TryParse(sessionId.ToString(), out var parsed))
            {
                await db.KeyDeleteAsync(GetSessionKey(parsed));
            }
        }

        await db.KeyDeleteAsync(indexKey);
    }

    public async Task UpdateLastActivityAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var db = await GetDbAsync();
        if (db is null)
            return;

        var key = GetSessionKey(sessionId);
        if (_options.SlidingExpirationMinutes <= 0)
            return;

        await db.KeyExpireAsync(key, TimeSpan.FromMinutes(_options.SlidingExpirationMinutes));
    }

    private async Task<IDatabase?> GetDbAsync()
    {
        var mux = await _lazyConnection.Value;
        return mux?.GetDatabase();
    }

    private static string GetSessionKey(Guid sessionId) => $"session:{sessionId:N}";
    private static string GetUserSessionsKey(Guid userId) => $"user-sessions:{userId:N}";
}
