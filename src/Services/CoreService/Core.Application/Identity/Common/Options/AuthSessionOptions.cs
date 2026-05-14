namespace Core.Application.Identity.Common.Options;

public class AuthSessionOptions
{
    public bool RedisEnabled { get; set; } = false;
    public bool ValidateOnEachRequest { get; set; } = false;
    public int SlidingExpirationMinutes { get; set; } = 60 * 24; // default 24h
    public int AbsoluteExpirationDays { get; set; } = 15; // align with refresh token default
}

