namespace Core.Application.Identity.Common.Options;

public class OtpOptions
{
    public bool RedisEnabled { get; set; } = false;
    public int TtlMinutes { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxAttempts { get; set; } = 5;
    public int LockoutSeconds { get; set; } = 15 * 60;
}

