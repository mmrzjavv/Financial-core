namespace Core.Application.Identity.Common.Options;

public class AuthSessionOptions
{
    public bool RedisEnabled { get; set; } = false;
    public bool ValidateOnEachRequest { get; set; } = false;
    public int SlidingExpirationMinutes { get; set; } = 60 * 24; // default 24h
    public int AbsoluteExpirationDays { get; set; } = 15; // align with refresh token default

    /// <summary>Maximum concurrent active sessions per user (oldest sessions revoked on new login).</summary>
    public int MaxActiveSessions { get; set; } = 3;

    /// <summary>Users with session activity within this window are considered online.</summary>
    public int OnlineActivityWindowMinutes { get; set; } = 30;
}

