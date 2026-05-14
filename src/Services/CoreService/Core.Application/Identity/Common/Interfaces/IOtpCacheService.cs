namespace Core.Application.Identity.Common.Interfaces;

public interface IOtpCacheService
{
    Task<OtpRequestDecision> CanRequestOtpAsync(string mobileNumber, CancellationToken cancellationToken);
    Task StoreOtpAsync(string mobileNumber, string otpCode, CancellationToken cancellationToken);
    Task<OtpValidationResult> ValidateOtpAsync(string mobileNumber, string otpCode, CancellationToken cancellationToken);
    Task InvalidateOtpAsync(string mobileNumber, CancellationToken cancellationToken);
}

public sealed record OtpRequestDecision(bool Allowed, int? RetryAfterSeconds, string? Reason);

public sealed record OtpValidationResult(
    bool Success,
    bool RedisUnavailable,
    bool Expired,
    bool Locked,
    int Attempts,
    int MaxAttempts,
    int? RetryAfterSeconds
);
