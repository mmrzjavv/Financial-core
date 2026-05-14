using System.Text.Json.Serialization;
using Services.CoreService.Core.Domain.Identity.Enums;

namespace Core.Application.Identity.Events.Auth;

[JsonDerivedType(typeof(UserRegisteredEvent), "UserRegistered")]
[JsonDerivedType(typeof(UserLoggedInEvent), "UserLoggedIn")]
[JsonDerivedType(typeof(UserLoggedOutEvent), "UserLoggedOut")]
[JsonDerivedType(typeof(OtpRequestedEvent), "OtpRequested")]
[JsonDerivedType(typeof(OtpVerifiedEvent), "OtpVerified")]
[JsonDerivedType(typeof(TokenRefreshedEvent), "TokenRefreshed")]
[JsonDerivedType(typeof(SessionRevokedEvent), "SessionRevoked")]
[JsonDerivedType(typeof(TokenReuseDetectedEvent), "TokenReuseDetected")]
[JsonDerivedType(typeof(SuspiciousActivityDetectedEvent), "SuspiciousActivityDetected")]
[JsonDerivedType(typeof(PasswordChangedEvent), "PasswordChanged")]
public abstract class AuthEvent : BaseAuthDomainEvent
{
    protected AuthEvent() { }

    protected AuthEvent(Guid? correlationId, Guid? traceId, string? userId, string? sessionId)
        : base(correlationId, traceId, userId, sessionId) { }
}

public sealed class UserRegisteredEvent : AuthEvent
{
    public override string EventType => "UserRegistered";

    public string PhoneNumber { get; }
    public string? NationalCode { get; }
    public string? FirstName { get; }
    public string? LastName { get; }
    public UserRole Role { get; }

    public UserRegisteredEvent(
        string phoneNumber,
        string? nationalCode,
        string? firstName,
        string? lastName,
        UserRole role,
        Guid? correlationId = null,
        Guid? traceId = null,
        string? userId = null,
        string? sessionId = null)
        : base(correlationId, traceId, userId, sessionId)
    {
        PhoneNumber = phoneNumber;
        NationalCode = nationalCode;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
    }
}

public sealed class UserLoggedInEvent : AuthEvent
{
    public override string EventType => "UserLoggedIn";

    public string PhoneNumber { get; }
    public string IpAddress { get; }
    public string UserAgent { get; }

    public UserLoggedInEvent(
        string phoneNumber,
        string ipAddress,
        string userAgent,
        Guid? correlationId = null,
        Guid? traceId = null,
        string? userId = null,
        string? sessionId = null)
        : base(correlationId, traceId, userId, sessionId)
    {
        PhoneNumber = phoneNumber;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}

public sealed class UserLoggedOutEvent : AuthEvent
{
    public override string EventType => "UserLoggedOut";

    public string PhoneNumber { get; }
    public string Reason { get; }

    public UserLoggedOutEvent(
        string phoneNumber,
        string reason,
        Guid? correlationId = null,
        Guid? traceId = null,
        string? userId = null,
        string? sessionId = null)
        : base(correlationId, traceId, userId, sessionId)
    {
        PhoneNumber = phoneNumber;
        Reason = reason;
    }
}

public sealed class OtpRequestedEvent : AuthEvent
{
    public override string EventType => "OtpRequested";

    public string PhoneNumber { get; }
    public string RequestType { get; } // "login", "register", etc.

    public OtpRequestedEvent(
        string phoneNumber,
        string requestType,
        Guid? correlationId = null,
        Guid? traceId = null,
        string? userId = null,
        string? sessionId = null)
        : base(correlationId, traceId, userId, sessionId)
    {
        PhoneNumber = phoneNumber;
        RequestType = requestType;
    }
}

public sealed class OtpVerifiedEvent : AuthEvent
{
    public override string EventType => "OtpVerified";

    public string PhoneNumber { get; }
    public bool Success { get; }
    public string? FailureReason { get; }

    public OtpVerifiedEvent(
        string phoneNumber,
        bool success,
        string? failureReason = null,
        Guid? correlationId = null,
        Guid? traceId = null,
        string? userId = null,
        string? sessionId = null)
        : base(correlationId, traceId, userId, sessionId)
    {
        PhoneNumber = phoneNumber;
        Success = success;
        FailureReason = failureReason;
    }
}

public sealed class TokenRefreshedEvent : AuthEvent
{
    public override string EventType => "TokenRefreshed";

    public string PhoneNumber { get; }
    public string IpAddress { get; }

    public TokenRefreshedEvent(
        string phoneNumber,
        string ipAddress,
        Guid? correlationId = null,
        Guid? traceId = null,
        string? userId = null,
        string? sessionId = null)
        : base(correlationId, traceId, userId, sessionId)
    {
        PhoneNumber = phoneNumber;
        IpAddress = ipAddress;
    }
}

public sealed class SessionRevokedEvent : AuthEvent
{
    public override string EventType => "SessionRevoked";

    public string PhoneNumber { get; }
    public string Reason { get; }

    public SessionRevokedEvent(
        string phoneNumber,
        string reason,
        Guid? correlationId = null,
        Guid? traceId = null,
        string? userId = null,
        string? sessionId = null)
        : base(correlationId, traceId, userId, sessionId)
    {
        PhoneNumber = phoneNumber;
        Reason = reason;
    }
}

public sealed class TokenReuseDetectedEvent : AuthEvent
{
    public override string EventType => "TokenReuseDetected";

    public string PhoneNumber { get; }
    public string IpAddress { get; }
    public string TokenHash { get; }

    public TokenReuseDetectedEvent(
        string phoneNumber,
        string ipAddress,
        string tokenHash,
        Guid? correlationId = null,
        Guid? traceId = null,
        string? userId = null,
        string? sessionId = null)
        : base(correlationId, traceId, userId, sessionId)
    {
        PhoneNumber = phoneNumber;
        IpAddress = ipAddress;
        TokenHash = tokenHash;
    }
}

public sealed class SuspiciousActivityDetectedEvent : AuthEvent
{
    public override string EventType => "SuspiciousActivityDetected";

    public string PhoneNumber { get; }
    public string ActivityType { get; }
    public string IpAddress { get; }
    public string Details { get; }

    public SuspiciousActivityDetectedEvent(
        string phoneNumber,
        string activityType,
        string ipAddress,
        string details,
        Guid? correlationId = null,
        Guid? traceId = null,
        string? userId = null,
        string? sessionId = null)
        : base(correlationId, traceId, userId, sessionId)
    {
        PhoneNumber = phoneNumber;
        ActivityType = activityType;
        IpAddress = ipAddress;
        Details = details;
    }
}

public sealed class PasswordChangedEvent : AuthEvent
{
    public override string EventType => "PasswordChanged";

    public string PhoneNumber { get; }
    public string IpAddress { get; }

    public PasswordChangedEvent(
        string phoneNumber,
        string ipAddress,
        Guid? correlationId = null,
        Guid? traceId = null,
        string? userId = null,
        string? sessionId = null)
        : base(correlationId, traceId, userId, sessionId)
    {
        PhoneNumber = phoneNumber;
        IpAddress = ipAddress;
    }
}
