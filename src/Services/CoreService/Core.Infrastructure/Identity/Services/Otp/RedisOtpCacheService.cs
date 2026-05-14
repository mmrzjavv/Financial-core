using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core.Application.Identity.Common.Interfaces;
using Core.Application.Identity.Common.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Core.Infrastructure.Identity.Services.Otp;

public class RedisOtpCacheService : IOtpCacheService
{
    private readonly OtpOptions _options;
    private readonly string? _redisConnectionString;
    private readonly IMemoryCache _memoryCache;

    private readonly Lazy<Task<ConnectionMultiplexer?>> _lazyConnection;

    public RedisOtpCacheService(IConfiguration configuration, IOptions<OtpOptions> options, IMemoryCache memoryCache)
    {
        _options = options.Value;
        _redisConnectionString = configuration.GetConnectionString("Redis");
        _memoryCache = memoryCache;

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

    public async Task<OtpRequestDecision> CanRequestOtpAsync(string mobileNumber, CancellationToken cancellationToken)
    {
        var db = await GetDbAsync();
        if (db is null)
            return CanRequestOtpFromMemory(mobileNumber);

        var key = GetKey(mobileNumber);
        var json = await db.StringGetAsync(key);
        if (!json.HasValue)
            return new OtpRequestDecision(true, null, null);

        var state = Deserialize(json!) ?? new OtpState();
        return EvaluateRequestDecision(state);
    }

    public async Task StoreOtpAsync(string mobileNumber, string otpCode, CancellationToken cancellationToken)
    {
        var db = await GetDbAsync();
        var state = CreateOtpState(otpCode);

        if (db is null)
        {
            _memoryCache.Set(GetKey(mobileNumber), state, BuildTtl());
            return;
        }

        await db.StringSetAsync(GetKey(mobileNumber), Serialize(state), BuildTtl());
    }

    public async Task<OtpValidationResult> ValidateOtpAsync(string mobileNumber, string otpCode, CancellationToken cancellationToken)
    {
        var db = await GetDbAsync();
        if (db is null)
            return ValidateOtpFromMemory(mobileNumber, otpCode);

        var key = GetKey(mobileNumber);
        var json = await db.StringGetAsync(key);
        if (!json.HasValue)
            return ExpiredResult();

        var state = Deserialize(json!) ?? new OtpState();
        var result = EvaluateValidation(state, otpCode);
        if (result.Success)
        {
            await db.KeyDeleteAsync(key);
            return result;
        }

        var ttl = await db.KeyTimeToLiveAsync(key) ?? BuildTtl();
        await db.StringSetAsync(key, Serialize(state), ttl);
        return result;
    }

    public async Task InvalidateOtpAsync(string mobileNumber, CancellationToken cancellationToken)
    {
        var key = GetKey(mobileNumber);
        var db = await GetDbAsync();
        if (db is null)
        {
            _memoryCache.Remove(key);
            return;
        }

        await db.KeyDeleteAsync(key);
    }

    private OtpRequestDecision CanRequestOtpFromMemory(string mobileNumber)
    {
        if (!_memoryCache.TryGetValue(GetKey(mobileNumber), out OtpState? state) || state is null)
            return new OtpRequestDecision(true, null, null);

        return EvaluateRequestDecision(state);
    }

    private OtpValidationResult ValidateOtpFromMemory(string mobileNumber, string otpCode)
    {
        var key = GetKey(mobileNumber);
        if (!_memoryCache.TryGetValue(key, out OtpState? state) || state is null)
            return ExpiredResult();

        var result = EvaluateValidation(state, otpCode);
        if (result.Success)
        {
            _memoryCache.Remove(key);
            return result;
        }

        _memoryCache.Set(key, state, BuildTtl());
        return result;
    }

    private OtpRequestDecision EvaluateRequestDecision(OtpState state)
    {
        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (state.LockedUntilUnixSeconds.HasValue && state.LockedUntilUnixSeconds.Value > nowUnix)
        {
            var retry = (int)(state.LockedUntilUnixSeconds.Value - nowUnix);
            return new OtpRequestDecision(false, retry, "locked");
        }

        if (state.CooldownUntilUnixSeconds.HasValue && state.CooldownUntilUnixSeconds.Value > nowUnix)
        {
            var retry = (int)(state.CooldownUntilUnixSeconds.Value - nowUnix);
            return new OtpRequestDecision(false, retry, "cooldown");
        }

        return new OtpRequestDecision(true, null, null);
    }

    private OtpValidationResult EvaluateValidation(OtpState state, string otpCode)
    {
        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (state.LockedUntilUnixSeconds.HasValue && state.LockedUntilUnixSeconds.Value > nowUnix)
        {
            var retry = (int)(state.LockedUntilUnixSeconds.Value - nowUnix);
            return new OtpValidationResult(false, RedisUnavailable: false, Expired: false, Locked: true, Attempts: state.Attempts, MaxAttempts: state.MaxAttempts, RetryAfterSeconds: retry);
        }

        var computed = ComputeHash(otpCode, state.Salt);
        var ok = FixedTimeEquals(state.Hash, computed);
        if (ok)
            return new OtpValidationResult(true, RedisUnavailable: false, Expired: false, Locked: false, Attempts: state.Attempts, MaxAttempts: state.MaxAttempts, RetryAfterSeconds: null);

        state.Attempts++;
        if (state.Attempts >= state.MaxAttempts)
            state.LockedUntilUnixSeconds = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, _options.LockoutSeconds)).ToUnixTimeSeconds();

        int? retryAfter = null;
        if (state.LockedUntilUnixSeconds.HasValue && state.LockedUntilUnixSeconds.Value > nowUnix)
            retryAfter = (int)(state.LockedUntilUnixSeconds.Value - nowUnix);

        return new OtpValidationResult(false, RedisUnavailable: false, Expired: false, Locked: retryAfter.HasValue, Attempts: state.Attempts, MaxAttempts: state.MaxAttempts, RetryAfterSeconds: retryAfter);
    }

    private OtpState CreateOtpState(string otpCode)
    {
        var now = DateTimeOffset.UtcNow;
        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        return new OtpState
        {
            Salt = salt,
            Hash = ComputeHash(otpCode, salt),
            Attempts = 0,
            MaxAttempts = Math.Max(1, _options.MaxAttempts),
            CreatedAtUnixSeconds = now.ToUnixTimeSeconds(),
            CooldownUntilUnixSeconds = now.AddSeconds(Math.Max(0, _options.ResendCooldownSeconds)).ToUnixTimeSeconds(),
            LockedUntilUnixSeconds = null
        };
    }

    private OtpValidationResult ExpiredResult() =>
        new(false, RedisUnavailable: false, Expired: true, Locked: false, Attempts: 0, MaxAttempts: _options.MaxAttempts, RetryAfterSeconds: null);

    private TimeSpan BuildTtl() => TimeSpan.FromMinutes(Math.Max(1, _options.TtlMinutes));

    private async Task<IDatabase?> GetDbAsync()
    {
        var mux = await _lazyConnection.Value;
        return mux?.GetDatabase();
    }

    private static string GetKey(string mobileNumber) => $"otp:{mobileNumber}";

    private static string Serialize(OtpState state) => JsonSerializer.Serialize(state);

    private static OtpState? Deserialize(RedisValue value)
    {
        try
        {
            return JsonSerializer.Deserialize<OtpState>(value.ToString()!);
        }
        catch
        {
            return null;
        }
    }

    private static bool FixedTimeEquals(string? leftBase64, string? rightBase64)
    {
        if (string.IsNullOrWhiteSpace(leftBase64) || string.IsNullOrWhiteSpace(rightBase64))
            return false;

        try
        {
            var left = Convert.FromBase64String(leftBase64);
            var right = Convert.FromBase64String(rightBase64);
            return CryptographicOperations.FixedTimeEquals(left, right);
        }
        catch
        {
            return false;
        }
    }

    private static string ComputeHash(string otpCode, string? saltBase64)
    {
        var saltBytes = string.IsNullOrWhiteSpace(saltBase64) ? RandomNumberGenerator.GetBytes(16) : Convert.FromBase64String(saltBase64);
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(otpCode);
        var data = new byte[saltBytes.Length + bytes.Length];
        Buffer.BlockCopy(saltBytes, 0, data, 0, saltBytes.Length);
        Buffer.BlockCopy(bytes, 0, data, saltBytes.Length, bytes.Length);
        var hash = sha.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }

    private sealed class OtpState
    {
        public string? Salt { get; set; }
        public string? Hash { get; set; }
        public int Attempts { get; set; }
        public int MaxAttempts { get; set; }
        public long CreatedAtUnixSeconds { get; set; }
        public long? CooldownUntilUnixSeconds { get; set; }
        public long? LockedUntilUnixSeconds { get; set; }
    }
}
