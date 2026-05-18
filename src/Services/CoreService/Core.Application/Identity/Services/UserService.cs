using System.Net;
using System.Security.Cryptography;
using FluentValidation;
using Mapster;
using BuildingBlocks.Application.Results;
using Core.Application.Identity.DTOs.User;
using Core.Application.Identity.Notifications;
using Core.Application.Abstractions;
using Core.Application.Identity.Abstractions;
using Core.Application.Identity.Interfaces;
using Core.Application.Identity.Common.Interfaces;
using Microsoft.Extensions.Options;
using Core.Application.Identity.Authorization;
using Core.Application.Identity.Common;
using Core.Application.Identity.Common.Options;
using Core.Application.Logging;
using Core.Domain.Identity.Entities;
using Core.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace Core.Application.Identity.Services;

public class UserService(
    ITokenHelper tokenHelper,
    ICoreUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUser,
    INotificationService notificationService,
    IOtpCacheService otpCacheService,
    ISessionCacheService sessionCacheService,
    ICurrentRequestContext requestContext,
    IRefreshTokenService refreshTokenService,
    IValidator<CreateUserDto> createUserValidator,
    IValidator<UpdateUserDto> updateUserValidator,
    IValidator<SendOtpDto> sendOtpValidator,
    IValidator<VerifyOtpDto> verifyOtpValidator,
    IOptions<OtpOptions> otpOptions,
    IOptions<AuthSessionOptions> sessionOptions,
    ILogger<UserService> logger) : IUserService
{
    public async Task<ApiOperationResult<UserDto>> SendOtpAsync(SendOtpDto dto)
    {
        var result = new ApiOperationResult<UserDto>();

        try
        {
            ApplicationLog.Started(logger, "SendOtp");

            var validation = await sendOtpValidator.ValidateAsync(dto, CancellationToken.None);
            if (!validation.IsValid)
            {
                ApplicationLog.Blocked(logger, "SendOtp", "validation failed");
                return result.Failed(IdentityMessages.ValidationFailed, validation.Errors.Select(e => e.ErrorMessage).ToList(), HttpStatusCode.BadRequest);
            }

            var user = await unitOfWork.Users.GetAsync(u => u.PhoneNumber == dto.PhoneNumber, disableTracking: false);
            if (user is null)
            {
                ApplicationLog.Blocked(logger, "SendOtp", "no user registered for this phone number");
                return result.Failed(IdentityMessages.UserNotFoundByPhone, HttpStatusCode.NotFound);
            }

            var decision = await otpCacheService.CanRequestOtpAsync(dto.PhoneNumber, CancellationToken.None);
            if (!decision.Allowed)
            {
                ApplicationLog.Blocked(logger, "SendOtp", "rate limit exceeded", user.Id.ToString());
                return result.Failed(IdentityMessages.OtpRateLimited, HttpStatusCode.BadRequest);
            }

            var otpCode = otpOptions.Value.DevBypassEnabled && !string.IsNullOrWhiteSpace(otpOptions.Value.DevCode)
                ? otpOptions.Value.DevCode
                : RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
            var validTime = DateTime.UtcNow.AddMinutes(Math.Max(1, otpOptions.Value.TtlMinutes));

            await otpCacheService.StoreOtpAsync(dto.PhoneNumber, otpCode, CancellationToken.None);
            await notificationService.SendOtpNotificationAsync(dto.PhoneNumber, otpCode, validTime, CancellationToken.None);

            await unitOfWork.Users.UpdateAsync(user);
            await unitOfWork.SaveChangesAsync();

            ApplicationLog.Completed(logger,
                "OTP sent to user {UserId} at phone {PhoneNumber}",
                user.Id, dto.PhoneNumber);

            return result.Succeed(IdentityMessages.OtpSent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SendOtp failed for phone {PhoneNumber}", dto.PhoneNumber);
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<LoginDto>> VerifyOtpAsync(VerifyOtpDto dto)
    {
        var result = new ApiOperationResult<LoginDto>();

        try
        {
            ApplicationLog.Started(logger, "VerifyOtp");

            var validationResult = await verifyOtpValidator.ValidateAsync(dto, CancellationToken.None);
            if (!validationResult.IsValid)
            {
                ApplicationLog.Blocked(logger, "VerifyOtp", "validation failed");
                return result.Failed(IdentityMessages.ValidationFailed, validationResult.Errors.Select(error => error.ErrorMessage).ToList(), HttpStatusCode.BadRequest);
            }

            var user = await unitOfWork.Users.GetAsync(u => u.PhoneNumber == dto.PhoneNumber, disableTracking: false);
            if (user is null)
            {
                ApplicationLog.Blocked(logger, "VerifyOtp", "no user registered for this phone number");
                return result.Failed(IdentityMessages.UserNotFound, HttpStatusCode.NotFound);
            }

            var otpValidation = await otpCacheService.ValidateOtpAsync(dto.PhoneNumber, dto.OtpCode, CancellationToken.None);
            if (!otpValidation.Success)
            {
                var reason = otpValidation.Locked ? "OTP locked" : otpValidation.Expired ? "OTP expired" : "invalid OTP code";
                ApplicationLog.Blocked(logger, "VerifyOtp", reason, user.Id.ToString());
                return BuildOtpFailure(result, otpValidation);
            }

            var sessionId = Guid.NewGuid();
            var tokenData = tokenHelper.GenerateToken(
                user.Id.ToString(),
                user.PhoneNumber,
                RoleClaimMapper.ToClaimRole(user.Role),
                sessionId.ToString("N"));

            await sessionCacheService.StoreSessionAsync(CreateSessionDescriptor(user.Id, sessionId), CancellationToken.None);

            user.IsPhoneVerified = true;
            user.LastLoginAt = DateTime.UtcNow;

            var refreshToken = refreshTokenService.CreateNew(
                userId: user.Id,
                sessionId: sessionId,
                familyId: Guid.NewGuid(),
                parentTokenId: null,
                rawRefreshToken: tokenData.RefreshToken,
                expiresAt: tokenData.RefreshTokenExpiration,
                requestContext: requestContext);

            await unitOfWork.RefreshTokens.AddAsync(refreshToken);
            await unitOfWork.UserSessions.AddAsync(CreateUserSession(user.Id, sessionId, refreshToken.Id));
            await unitOfWork.Users.UpdateAsync(user);
            await unitOfWork.SaveChangesAsync();

            ApplicationLog.Completed(logger,
                "User {UserId} logged in via OTP — session {SessionId}, role {Role}",
                user.Id, sessionId, user.Role);

            var response = new LoginDto
            {
                TokenModel = tokenData,
                User = user.Adapt<UserDto>()
            };

            return result.Succeed(IdentityMessages.LoginSucceeded, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "VerifyOtp failed for phone {PhoneNumber}", dto.PhoneNumber);
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<LoginDto>> RefreshTokenAsync(RefreshTokenDto dto, string? accessToken)
    {
        var result = new ApiOperationResult<LoginDto>();

        try
        {
            ApplicationLog.Started(logger, "RefreshToken");

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                ApplicationLog.Blocked(logger, "RefreshToken", "access token is missing");
                return result.Failed(IdentityMessages.InvalidAccessToken, HttpStatusCode.BadRequest);
            }

            if (dto is null || string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                ApplicationLog.Blocked(logger, "RefreshToken", "refresh token is missing");
                return result.Failed(IdentityMessages.RefreshTokenRequired, HttpStatusCode.BadRequest);
            }

            var payload = tokenHelper.ValidateAccessToken(accessToken);
            if (payload.UserId == Guid.Empty)
            {
                ApplicationLog.Blocked(logger, "RefreshToken", "access token is invalid");
                return result.Failed(IdentityMessages.InvalidAccessToken, HttpStatusCode.BadRequest);
            }

            var user = await unitOfWork.Users.GetAsync(u => u.Id == payload.UserId, disableTracking: false);
            if (user is null)
            {
                ApplicationLog.Blocked(logger, "RefreshToken", "user not found", payload.UserId.ToString());
                return result.Failed(IdentityMessages.UserNotFound, HttpStatusCode.NotFound);
            }

            var presentedHash = refreshTokenService.Hash(dto.RefreshToken);
            var stored = await unitOfWork.RefreshTokens.GetByTokenHashAsync(presentedHash, disableTracking: false);
            if (stored is null || stored.UserId != user.Id)
            {
                ApplicationLog.Blocked(logger, "RefreshToken", "refresh token is invalid", user.Id.ToString());
                return result.Failed(IdentityMessages.InvalidRefreshToken, HttpStatusCode.BadRequest);
            }

            var now = DateTime.UtcNow;
            if (stored.RevokedAt is not null)
            {
                await InvalidateFamilyAsync(user.Id, stored.FamilyId, "reuse_detected", CancellationToken.None);

                logger.LogCritical(
                    "Security audit: refresh token reuse detected for user {UserId} — family {FamilyId}, session {SessionId} revoked",
                    user.Id, stored.FamilyId, stored.SessionId);

                return result.Failed(IdentityMessages.InvalidRefreshToken, HttpStatusCode.BadRequest);
            }

            if (stored.ExpiresAt <= now)
            {
                ApplicationLog.Blocked(logger, "RefreshToken", "refresh token expired", user.Id.ToString());
                return result.Failed(IdentityMessages.RefreshTokenExpired, HttpStatusCode.BadRequest);
            }

            var sessionId = stored.SessionId;

            if (stored.FamilyId == Guid.Empty)
            {
                stored.FamilyId = Guid.NewGuid();
                await unitOfWork.RefreshTokens.UpdateAsync(stored);
            }

            var tokenData = tokenHelper.GenerateToken(
                user.Id.ToString(),
                user.PhoneNumber,
                RoleClaimMapper.ToClaimRole(user.Role),
                sessionId.ToString("N"));

            var newRefresh = refreshTokenService.CreateNew(
                userId: user.Id,
                sessionId: sessionId,
                familyId: stored.FamilyId,
                parentTokenId: stored.Id,
                rawRefreshToken: tokenData.RefreshToken,
                expiresAt: tokenData.RefreshTokenExpiration,
                requestContext: requestContext);

            refreshTokenService.Revoke(stored, replacedByTokenId: newRefresh.Id, reason: "rotated", revokedByIp: requestContext.IpAddress);

            user.LastLoginAt = DateTime.UtcNow;

            await unitOfWork.RefreshTokens.AddAsync(newRefresh);
            await unitOfWork.RefreshTokens.UpdateAsync(stored);
            await unitOfWork.Users.UpdateAsync(user);

            var dbSession = await unitOfWork.UserSessions.GetBySessionIdAsync(sessionId, disableTracking: false);
            if (dbSession is not null && dbSession.RevokedAt is null)
            {
                dbSession.CurrentRefreshTokenId = newRefresh.Id;
                dbSession.LastActivityAt = DateTime.UtcNow;
                await unitOfWork.UserSessions.UpdateAsync(dbSession);
            }

            await unitOfWork.SaveChangesAsync();
            await sessionCacheService.UpdateLastActivityAsync(sessionId, CancellationToken.None);

            ApplicationLog.Completed(logger,
                "User {UserId} refreshed access token for session {SessionId}",
                user.Id, sessionId);

            var response = new LoginDto
            {
                TokenModel = tokenData,
                User = user.Adapt<UserDto>()
            };

            return result.Succeed(IdentityMessages.OperationSucceeded, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RefreshToken failed");
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> LogoutCurrentSessionAsync(CancellationToken cancellationToken = default)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            var userId = currentUser.UserId ?? Guid.Empty;
            var sessionId = currentUser.SessionId ?? Guid.Empty;

            ApplicationLog.Started(logger, "Logout", userId == Guid.Empty ? null : userId.ToString());

            if (userId == Guid.Empty || sessionId == Guid.Empty)
            {
                ApplicationLog.Blocked(logger, "Logout", "session identifiers are missing");
                return result.Failed(IdentityMessages.InvalidSessionIdentifiers, HttpStatusCode.BadRequest);
            }

            var dbSession = await unitOfWork.UserSessions.GetBySessionIdAsync(sessionId, disableTracking: false);
            if (dbSession is not null && dbSession.UserId == userId && dbSession.RevokedAt is null)
            {
                dbSession.RevokedAt = DateTime.UtcNow;
                dbSession.LastActivityAt = DateTime.UtcNow;
                await unitOfWork.UserSessions.UpdateAsync(dbSession);
            }

            var tokens = await unitOfWork.RefreshTokens.GetAllAsync(
                t => t.UserId == userId && t.SessionId == sessionId && t.RevokedAt == null,
                disableTracking: false);
            foreach (var token in tokens)
            {
                refreshTokenService.Revoke(token, replacedByTokenId: null, reason: "logout", revokedByIp: requestContext.IpAddress);
                await unitOfWork.RefreshTokens.UpdateAsync(token);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await sessionCacheService.RevokeSessionAsync(sessionId, cancellationToken);

            ApplicationLog.Completed(logger,
                "User {UserId} logged out from session {SessionId}",
                userId, sessionId);

            return result.Succeed(IdentityMessages.LogoutSucceeded);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Logout failed");
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> RevokeSessionAsync(RevokeSessionDto dto, CancellationToken cancellationToken = default)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            var userId = currentUser.UserId ?? Guid.Empty;
            var sessionId = dto.SessionId;

            ApplicationLog.Started(logger, "RevokeSession", userId == Guid.Empty ? null : userId.ToString());

            if (userId == Guid.Empty)
            {
                ApplicationLog.Blocked(logger, "RevokeSession", "user is not authenticated");
                return result.Failed(IdentityMessages.AuthenticationRequired, HttpStatusCode.Unauthorized);
            }

            var dbSession = await unitOfWork.UserSessions.GetBySessionIdAsync(sessionId, disableTracking: false);
            if (dbSession is null || dbSession.RevokedAt is not null)
            {
                ApplicationLog.Completed(logger,
                    "User {UserId} revoked session {SessionId} — already inactive",
                    userId, sessionId);
                return result.Succeed(IdentityMessages.OperationSucceeded);
            }

            if (dbSession.UserId != userId)
            {
                ApplicationLog.Blocked(logger, "RevokeSession", "session belongs to another user", userId.ToString());
                return result.Failed(IdentityMessages.SessionAccessDenied, HttpStatusCode.Forbidden);
            }

            dbSession.RevokedAt = DateTime.UtcNow;
            dbSession.LastActivityAt = DateTime.UtcNow;
            await unitOfWork.UserSessions.UpdateAsync(dbSession);

            var tokens = await unitOfWork.RefreshTokens.GetAllAsync(
                t => t.UserId == dbSession.UserId && t.SessionId == dbSession.SessionId && t.RevokedAt == null,
                disableTracking: false);
            foreach (var token in tokens)
            {
                refreshTokenService.Revoke(token, replacedByTokenId: null, reason: "session_revoked", revokedByIp: requestContext.IpAddress);
                await unitOfWork.RefreshTokens.UpdateAsync(token);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await sessionCacheService.RevokeSessionAsync(dbSession.SessionId, cancellationToken);

            ApplicationLog.Completed(logger,
                "User {UserId} revoked session {SessionId}",
                userId, sessionId);

            return result.Succeed(IdentityMessages.OperationSucceeded);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RevokeSession failed for session {SessionId}", dto.SessionId);
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> RevokeAllSessionsAsync(CancellationToken cancellationToken = default)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            var userId = currentUser.UserId ?? Guid.Empty;

            ApplicationLog.Started(logger, "RevokeAllSessions", userId == Guid.Empty ? null : userId.ToString());

            if (userId == Guid.Empty)
            {
                ApplicationLog.Blocked(logger, "RevokeAllSessions", "user is not authenticated");
                return result.Failed(IdentityMessages.AuthenticationRequired, HttpStatusCode.Unauthorized);
            }

            var sessions = await unitOfWork.UserSessions.GetAllAsync(s => s.UserId == userId && s.RevokedAt == null, disableTracking: false);
            foreach (var session in sessions)
            {
                session.RevokedAt = DateTime.UtcNow;
                session.LastActivityAt = DateTime.UtcNow;
                await unitOfWork.UserSessions.UpdateAsync(session);
            }

            var tokens = await unitOfWork.RefreshTokens.GetAllAsync(t => t.UserId == userId && t.RevokedAt == null, disableTracking: false);
            foreach (var token in tokens)
            {
                refreshTokenService.Revoke(token, replacedByTokenId: null, reason: "revoke_all_sessions", revokedByIp: requestContext.IpAddress);
                await unitOfWork.RefreshTokens.UpdateAsync(token);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await sessionCacheService.RevokeAllSessionsAsync(userId, cancellationToken);

            ApplicationLog.Completed(logger,
                "User {UserId} revoked all active sessions ({SessionCount} session(s))",
                userId, sessions.Count);

            return result.Succeed(IdentityMessages.OperationSucceeded);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RevokeAllSessions failed");
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<SessionDto>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        var result = new ApiOperationResult<SessionDto>();
        try
        {
            var userId = currentUser.UserId ?? Guid.Empty;

            ApplicationLog.Started(logger, "GetActiveSessions", userId == Guid.Empty ? null : userId.ToString());

            if (userId == Guid.Empty)
            {
                ApplicationLog.Blocked(logger, "GetActiveSessions", "user is not authenticated");
                return result.Failed(IdentityMessages.AuthenticationRequired, HttpStatusCode.Unauthorized);
            }

            var sessions = await unitOfWork.UserSessions.GetActiveByUserAsync(userId);
            var list = sessions.Select(s =>
            {
                var dto = s.Adapt<SessionDto>();
                dto.IsActive = s.RevokedAt is null;
                return dto;
            }).ToList();

            ApplicationLog.Completed(logger,
                "User {UserId} loaded {Count} active session(s)",
                userId, list.Count);

            return result.Succeed(IdentityMessages.OperationSucceeded, list, list.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetActiveSessions failed");
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> CreateAsync(CreateUserDto dto)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            ApplicationLog.Started(logger, "CreateUser");

            var validation = await createUserValidator.ValidateAsync(dto, CancellationToken.None);
            if (!validation.IsValid)
            {
                ApplicationLog.Blocked(logger, "CreateUser", "validation failed");
                return result.Failed(IdentityMessages.ValidationFailed, validation.Errors.Select(error => error.ErrorMessage).ToList(), HttpStatusCode.BadRequest);
            }

            var existPhone = await unitOfWork.Users.GetAsync(user => user.PhoneNumber == dto.PhoneNumber);
            if (existPhone is not null)
            {
                ApplicationLog.Blocked(logger, "CreateUser", "phone number already registered");
                return result.Failed(IdentityMessages.PhoneAlreadyRegistered, HttpStatusCode.Conflict);
            }

            var existEmail = await unitOfWork.Users.GetAsync(user => user.Email == dto.Email);
            if (existEmail is not null)
            {
                ApplicationLog.Blocked(logger, "CreateUser", "email already registered");
                return result.Failed(IdentityMessages.EmailAlreadyRegistered, HttpStatusCode.Conflict);
            }

            var existNationalCode = string.IsNullOrWhiteSpace(dto.NationalCode)
                ? null
                : await unitOfWork.Users.GetAsync(user => user.NationalCode == dto.NationalCode);
            if (existNationalCode is not null)
            {
                ApplicationLog.Blocked(logger, "CreateUser", "national code already registered");
                return result.Failed(IdentityMessages.NationalCodeAlreadyRegistered, HttpStatusCode.Conflict);
            }

            var user = dto.Adapt<User>();
            user.CreatedAt = DateTime.UtcNow;
            user.Role = otpOptions.Value.DevBypassEnabled
                && !string.IsNullOrWhiteSpace(otpOptions.Value.SeedAdminPhone)
                && string.Equals(dto.PhoneNumber, otpOptions.Value.SeedAdminPhone, StringComparison.Ordinal)
                ? UserRole.Admin
                : UserRole.Applicant;

            await unitOfWork.Users.AddAsync(user);
            await unitOfWork.SaveChangesAsync();

            ApplicationLog.Completed(logger,
                "New user {UserId} registered with role {Role} and phone {PhoneNumber}",
                user.Id, user.Role, user.PhoneNumber);

            return result.Succeed(IdentityMessages.UserCreated, user.Adapt<UserDto>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CreateUser failed");
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            var requesterId = currentUser.UserId ?? Guid.Empty;
            if (requesterId == Guid.Empty)
            {
                ApplicationLog.Blocked(logger, "UpdateUser", "user is not authenticated");
                return result.Failed(IdentityMessages.AuthenticationRequired, HttpStatusCode.Unauthorized);
            }

            ApplicationLog.Started(logger, "UpdateUser", requesterId.ToString());

            var validation = await updateUserValidator.ValidateAsync(dto, cancellationToken);
            if (!validation.IsValid)
            {
                ApplicationLog.Blocked(logger, "UpdateUser", "validation failed", requesterId.ToString());
                return result.Failed(IdentityMessages.ValidationFailed, validation.Errors.Select(e => e.ErrorMessage).ToList(), HttpStatusCode.BadRequest);
            }

            var user = await unitOfWork.Users.GetAsync(u => u.Id == id && !u.IsDeleted, false);
            if (user is null)
            {
                ApplicationLog.Blocked(logger, "UpdateUser", "target user not found", requesterId.ToString());
                return result.Failed(IdentityMessages.UserNotFound, HttpStatusCode.NotFound);
            }

            var requester = await unitOfWork.Users.GetAsync(u => u.Id == requesterId && !u.IsDeleted, false);
            if (dto.Role.HasValue && user.Role != dto.Role.Value && requester is not null && requester.Role != UserRole.Admin)
            {
                ApplicationLog.Blocked(logger, "UpdateUser", "only admin may change role", requesterId.ToString());
                return result.Failed(IdentityMessages.OnlyAdminCanChangeRole, HttpStatusCode.BadRequest);
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && dto.PhoneNumber != user.PhoneNumber)
            {
                var phoneExists = await unitOfWork.Users.GetAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != id);
                if (phoneExists is not null)
                {
                    ApplicationLog.Blocked(logger, "UpdateUser", "phone number already in use", requesterId.ToString());
                    return result.Failed(IdentityMessages.PhoneAlreadyRegistered, HttpStatusCode.Conflict);
                }

                user.PhoneNumber = dto.PhoneNumber;
            }

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
            {
                var emailExists = await unitOfWork.Users.GetAsync(u => u.Email == dto.Email && u.Id != id);
                if (emailExists is not null)
                {
                    ApplicationLog.Blocked(logger, "UpdateUser", "email already in use", requesterId.ToString());
                    return result.Failed(IdentityMessages.EmailAlreadyRegistered, HttpStatusCode.Conflict);
                }

                user.Email = dto.Email;
            }

            if (!string.IsNullOrWhiteSpace(dto.NationalCode) && dto.NationalCode != user.NationalCode)
            {
                var nationalCodeExists = await unitOfWork.Users.GetAsync(u => u.NationalCode == dto.NationalCode && u.Id != id);
                if (nationalCodeExists is not null)
                {
                    ApplicationLog.Blocked(logger, "UpdateUser", "national code already in use", requesterId.ToString());
                    return result.Failed(IdentityMessages.NationalCodeAlreadyRegistered, HttpStatusCode.Conflict);
                }

                user.NationalCode = dto.NationalCode;
            }

            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                user.FirstName = dto.FirstName;

            if (!string.IsNullOrWhiteSpace(dto.LastName))
                user.LastName = dto.LastName;

            if (dto.Role.HasValue) user.Role = dto.Role.Value;
            if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
            if (dto.IsPhoneVerified.HasValue) user.IsPhoneVerified = dto.IsPhoneVerified.Value;
            if (dto.LastLoginAt.HasValue) user.LastLoginAt = dto.LastLoginAt;

            await unitOfWork.Users.UpdateAsync(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            ApplicationLog.Completed(logger,
                "User {RequesterId} updated profile of user {TargetUserId} (role {Role})",
                requesterId, id, user.Role);

            return result.Succeed(IdentityMessages.UserUpdated, user.Adapt<UserDto>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "UpdateUser failed for user {UserId}", id);
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> GetByIdAsync(Guid id)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            ApplicationLog.Started(logger, "GetUserById");

            var user = await unitOfWork.Users.GetAsync(u => u.Id == id && !u.IsDeleted);
            if (user is null)
            {
                ApplicationLog.Blocked(logger, "GetUserById", "user not found");
                return result.Failed(IdentityMessages.UserNotFound, HttpStatusCode.NotFound);
            }

            ApplicationLog.Completed(logger, "Loaded user {UserId}", id);
            return result.Succeed(IdentityMessages.OperationSucceeded, user.Adapt<UserDto>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetUserById failed for user {UserId}", id);
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> GetPagedAsync(int take, int skip)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            ApplicationLog.Started(logger, "GetUsersPaged");

            var users = await unitOfWork.Users.GetPagedListAsync(take, skip, u => !u.IsDeleted);
            var total = await unitOfWork.Users.CountAsync(u => !u.IsDeleted);
            var list = users.Select(u => u.Adapt<UserDto>()).ToList();

            ApplicationLog.Completed(logger,
                "Listed users page skip={Skip}, take={Take} — {Count} of {Total}",
                skip, take, list.Count, total);

            return result.Succeed(IdentityMessages.OperationSucceeded, list, total);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetUsersPaged failed");
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> DeleteAsync(Guid id)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            ApplicationLog.Started(logger, "DeleteUser");

            var user = await unitOfWork.Users.GetAsync(u => u.Id == id && !u.IsDeleted, false);
            if (user is null)
            {
                ApplicationLog.Blocked(logger, "DeleteUser", "user not found");
                return result.Failed(IdentityMessages.UserNotFound, HttpStatusCode.NotFound);
            }

            await unitOfWork.Users.DeleteAsync(user);
            await unitOfWork.SaveChangesAsync();

            ApplicationLog.Completed(logger, "User {UserId} was soft-deleted", id);
            return result.Succeed(IdentityMessages.UserDeleted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DeleteUser failed for user {UserId}", id);
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            var userId = currentUser.UserId ?? Guid.Empty;

            ApplicationLog.Started(logger, "GetProfile", userId == Guid.Empty ? null : userId.ToString());

            if (userId == Guid.Empty)
            {
                ApplicationLog.Blocked(logger, "GetProfile", "user is not authenticated");
                return result.Failed(IdentityMessages.AuthenticationRequired, HttpStatusCode.Unauthorized);
            }

            var user = await unitOfWork.Users.GetAsync(u => u.Id == userId && !u.IsDeleted, false);

            ApplicationLog.Completed(logger,
                "User {UserId} loaded own profile",
                userId);

            return result.Succeed(IdentityMessages.OperationSucceeded, user is null ? new UserDto() : user.Adapt<UserDto>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetProfile failed");
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    private static ApiOperationResult<LoginDto> BuildOtpFailure(
        ApiOperationResult<LoginDto> result,
        OtpValidationResult otpValidation)
    {
        if (otpValidation.Locked)
            return result.Failed(IdentityMessages.OtpLocked, HttpStatusCode.BadRequest);

        if (otpValidation.Expired)
            return result.Failed(IdentityMessages.OtpExpired, HttpStatusCode.BadRequest);

        return result.Failed(IdentityMessages.InvalidOtp, HttpStatusCode.BadRequest);
    }

    private SessionDescriptor CreateSessionDescriptor(Guid userId, Guid sessionId)
    {
        var now = DateTimeOffset.UtcNow;
        return new SessionDescriptor(
            SessionId: sessionId,
            UserId: userId,
            IpAddress: requestContext.IpAddress,
            UserAgent: requestContext.UserAgent,
            CreatedAt: now,
            ExpiresAt: now.AddDays(Math.Max(1, sessionOptions.Value.AbsoluteExpirationDays)));
    }

    private UserSession CreateUserSession(Guid userId, Guid sessionId, Guid refreshTokenId)
    {
        var now = DateTime.UtcNow;
        return new UserSession
        {
            SessionId = sessionId,
            UserId = userId,
            CurrentRefreshTokenId = refreshTokenId,
            DeviceId = requestContext.DeviceId,
            UserAgent = requestContext.UserAgent,
            IpAddress = requestContext.IpAddress,
            CreatedAt = now,
            LastActivityAt = now
        };
    }

    private async Task InvalidateFamilyAsync(Guid userId, Guid familyId, string reason, CancellationToken cancellationToken)
    {
        var tokens = await unitOfWork.RefreshTokens.GetAllAsync(t => t.UserId == userId && t.FamilyId == familyId, disableTracking: false);
        foreach (var token in tokens)
        {
            if (token.RevokedAt is null)
            {
                refreshTokenService.Revoke(token, replacedByTokenId: null, reason: reason, revokedByIp: requestContext.IpAddress);
                await unitOfWork.RefreshTokens.UpdateAsync(token);
            }
        }

        await sessionCacheService.RevokeAllSessionsAsync(userId, cancellationToken);

        var sessions = await unitOfWork.UserSessions.GetAllAsync(s => s.UserId == userId && s.RevokedAt == null, disableTracking: false);
        foreach (var session in sessions)
        {
            session.RevokedAt = DateTime.UtcNow;
            await unitOfWork.UserSessions.UpdateAsync(session);
        }
    }
}
