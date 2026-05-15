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
using Services.CoreService.Core.Domain.Identity.Entities;
using Services.CoreService.Core.Domain.Identity.Enums;
using Microsoft.Extensions.Options;
using Core.Application.Identity.Authorization;
using Core.Application.Identity.Common;
using Core.Application.Identity.Common.Options;


namespace Core.Application.Identity.Services;

public class UserService : IUserService
{
    private readonly ITokenHelper _tokenHelper;
    private readonly ICoreUnitOfWork _unitOfWork;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly INotificationService _notificationService;
    private readonly IOtpCacheService _otpCacheService;
    private readonly ISessionCacheService _sessionCacheService;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuditEventLogger _auditEventLogger;
    private readonly IValidator<CreateUserDto> _createUserValidator;
    private readonly IValidator<UpdateUserDto> _updateUserValidator;
    private readonly IValidator<SendOtpDto> _sendOtpValidator;
    private readonly IValidator<VerifyOtpDto> _verifyOtpValidator;
    private readonly IOptions<OtpOptions> _otpOptions;
    private readonly IOptions<AuthSessionOptions> _sessionOptions;
    private readonly IStructuredLogger _logger;

    public UserService(
        ITokenHelper tokenHelper,
        ICoreUnitOfWork unitOfWork,
        ICurrentUserAccessor currentUser,
        INotificationService notificationService,
        IOtpCacheService otpCacheService,
        ISessionCacheService sessionCacheService,
        ICurrentRequestContext requestContext,
        IRefreshTokenService refreshTokenService,
        IAuditEventLogger auditEventLogger,
        IValidator<CreateUserDto> createUserValidator,
        IValidator<UpdateUserDto> updateUserValidator,
        IValidator<SendOtpDto> sendOtpValidator,
        IValidator<VerifyOtpDto> verifyOtpValidator,
        IOptions<OtpOptions> otpOptions,
        IOptions<AuthSessionOptions> sessionOptions,
        IStructuredLogger logger)
    {
        _tokenHelper = tokenHelper;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _notificationService = notificationService;
        _otpCacheService = otpCacheService;
        _sessionCacheService = sessionCacheService;
        _requestContext = requestContext;
        _refreshTokenService = refreshTokenService;
        _auditEventLogger = auditEventLogger;
        _createUserValidator = createUserValidator;
        _updateUserValidator = updateUserValidator;
        _sendOtpValidator = sendOtpValidator;
        _verifyOtpValidator = verifyOtpValidator;
        _otpOptions = otpOptions;
        _sessionOptions = sessionOptions;
        _logger = logger;
    }

    public async Task<ApiOperationResult<UserDto>> SendOtpAsync(SendOtpDto dto)
    {
        var result = new ApiOperationResult<UserDto>();

        try
        {
            var validation = await _sendOtpValidator.ValidateAsync(dto, CancellationToken.None);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("OTP send validation failed", new Dictionary<string, object>
                {
                    { "PhoneNumber", dto.PhoneNumber },
                    { "Errors", errors }
                });
                return result.Failed(IdentityMessages.ValidationFailed, errors, HttpStatusCode.BadRequest);
            }

            var user = await _unitOfWork.Users.GetAsync(u => u.PhoneNumber == dto.PhoneNumber, disableTracking: false);
            if (user is null)
            {
                _logger.LogWarning("OTP request for non-existent user", new Dictionary<string, object>
                {
                    { "PhoneNumber", dto.PhoneNumber }
                });
                return result.Failed(IdentityMessages.UserNotFoundByPhone, HttpStatusCode.NotFound);
            }

            var decision = await _otpCacheService.CanRequestOtpAsync(dto.PhoneNumber, CancellationToken.None);
            if (!decision.Allowed)
            {
                _logger.LogWarning("OTP request rate limited", new Dictionary<string, object>
                {
                    { "PhoneNumber", dto.PhoneNumber }
                });
                return result.Failed(IdentityMessages.OtpRateLimited, HttpStatusCode.BadRequest);
            }

            var otpCode = _otpOptions.Value.DevBypassEnabled && !string.IsNullOrWhiteSpace(_otpOptions.Value.DevCode)
                ? _otpOptions.Value.DevCode
                : RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
            var validTime = DateTime.UtcNow.AddMinutes(Math.Max(1, _otpOptions.Value.TtlMinutes));

            await _otpCacheService.StoreOtpAsync(dto.PhoneNumber, otpCode, CancellationToken.None);
            await _notificationService.SendOtpNotificationAsync(dto.PhoneNumber, otpCode, validTime, CancellationToken.None);

            _logger.LogAuthEvent("SendOtp", dto.PhoneNumber, true);

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return result.Succeed(IdentityMessages.OtpSent);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send OTP", ex, new Dictionary<string, object>
            {
                { "PhoneNumber", dto.PhoneNumber }
            });
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<LoginDto>> VerifyOtpAsync(VerifyOtpDto dto)
    {
        var result = new ApiOperationResult<LoginDto>();

        try
        {
            var validationResult = await _verifyOtpValidator.ValidateAsync(dto, CancellationToken.None);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
                _logger.LogWarning("OTP verification validation failed", new Dictionary<string, object>
                {
                    { "PhoneNumber", dto.PhoneNumber },
                    { "Errors", errors }
                });
                return result.Failed(IdentityMessages.ValidationFailed, errors, HttpStatusCode.BadRequest);
            }

            var user = await _unitOfWork.Users.GetAsync(user => user.PhoneNumber == dto.PhoneNumber, disableTracking: false);
            if (user is null)
            {
                _logger.LogWarning("OTP verification for non-existent user", new Dictionary<string, object>
                {
                    { "PhoneNumber", dto.PhoneNumber }
                });
                return result.Failed(IdentityMessages.UserNotFound, HttpStatusCode.NotFound);
            }

            var otpValidation = await _otpCacheService.ValidateOtpAsync(dto.PhoneNumber, dto.OtpCode, CancellationToken.None);
            if (!otpValidation.Success)
            {
                _logger.LogAuthEvent("VerifyOtp", dto.PhoneNumber, false,
                    otpValidation.Locked ? "Locked" : otpValidation.Expired ? "Expired" : "InvalidCode");
                return BuildOtpFailure(result, otpValidation);
            }

            var sessionId = Guid.NewGuid();
            var tokenData = _tokenHelper.GenerateToken(
                user.Id.ToString(),
                user.PhoneNumber,
                RoleClaimMapper.ToClaimRole(user.Role),
                sessionId.ToString("N"));

            await _sessionCacheService.StoreSessionAsync(CreateSessionDescriptor(user.Id, sessionId), CancellationToken.None);

            user.IsPhoneVerified = true;
            user.LastLoginAt = DateTime.UtcNow;

            var refreshToken = _refreshTokenService.CreateNew(
                userId: user.Id,
                sessionId: sessionId,
                familyId: Guid.NewGuid(),
                parentTokenId: null,
                rawRefreshToken: tokenData.RefreshToken,
                expiresAt: tokenData.RefreshTokenExpiration,
                requestContext: _requestContext);

            await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
            await _unitOfWork.UserSessions.AddAsync(CreateUserSession(user.Id, sessionId, refreshToken.Id));
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogAuthEvent("VerifyOtp", dto.PhoneNumber, true, additionalProperties: new Dictionary<string, object>
            {
                { "UserId", user.Id },
                { "SessionId", sessionId }
            });

            var response = new LoginDto
            {
                TokenModel = tokenData,
                User = user.Adapt<UserDto>()
            };

            return result.Succeed(IdentityMessages.LoginSucceeded, response);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to verify OTP", ex, new Dictionary<string, object>
            {
                { "PhoneNumber", dto.PhoneNumber }
            });
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<LoginDto>> RefreshTokenAsync(RefreshTokenDto dto, string? accessToken)
    {
        var result = new ApiOperationResult<LoginDto>();

        try
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return result.Failed(IdentityMessages.InvalidAccessToken, HttpStatusCode.BadRequest);
            if (dto is null || string.IsNullOrWhiteSpace(dto.RefreshToken))
                return result.Failed(IdentityMessages.RefreshTokenRequired, HttpStatusCode.BadRequest);

            var payload = _tokenHelper.ValidateAccessToken(accessToken);
            if (payload.UserId == Guid.Empty)
            {
                _logger.LogWarning("Invalid access token presented for refresh");
                return result.Failed(IdentityMessages.InvalidAccessToken, HttpStatusCode.BadRequest);
            }

            var user = await _unitOfWork.Users.GetAsync(u => u.Id == payload.UserId, disableTracking: false);
            if (user is null)
            {
                _logger.LogWarning("Refresh token requested for non-existent user", new Dictionary<string, object>
                {
                    { "UserId", payload.UserId }
                });
                return result.Failed(IdentityMessages.UserNotFound, HttpStatusCode.NotFound);
            }

            var presentedHash = _refreshTokenService.Hash(dto.RefreshToken);
            var stored = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(presentedHash, disableTracking: false);
            if (stored is null || stored.UserId != user.Id)
            {
                _logger.LogAuthEvent("RefreshToken", user.PhoneNumber, false, "InvalidRefreshToken");
                return result.Failed(IdentityMessages.InvalidRefreshToken, HttpStatusCode.BadRequest);
            }

            var now = DateTime.UtcNow;
            if (stored.RevokedAt is not null)
            {
                await InvalidateFamilyAsync(user.Id, stored.FamilyId, "reuse_detected", CancellationToken.None);

                _logger.LogCritical("Token reuse detected! Revoking token family.", null, new Dictionary<string, object>
                {
                    { "UserId", user.Id },
                    { "FamilyId", stored.FamilyId },
                    { "SessionId", stored.SessionId }
                });

                await _auditEventLogger.LogAsync(
                    new AuditEvent(
                        EventName: "TokenReuseDetected",
                        UserId: user.Id,
                        SessionId: stored.SessionId,
                        Success: false,
                        Reason: "reuse_detected"),
                    CancellationToken.None);

                return result.Failed(IdentityMessages.InvalidRefreshToken, HttpStatusCode.BadRequest);
            }

            if (stored.ExpiresAt <= now)
            {
                _logger.LogAuthEvent("RefreshToken", user.PhoneNumber, false, "Expired");
                return result.Failed(IdentityMessages.RefreshTokenExpired, HttpStatusCode.BadRequest);
            }

            var sessionId = stored.SessionId;

            if (stored.FamilyId == Guid.Empty)
            {
                stored.FamilyId = Guid.NewGuid();
                await _unitOfWork.RefreshTokens.UpdateAsync(stored);
            }

            var tokenData = _tokenHelper.GenerateToken(
                user.Id.ToString(),
                user.PhoneNumber,
                RoleClaimMapper.ToClaimRole(user.Role),
                sessionId.ToString("N"));

            var newRefresh = _refreshTokenService.CreateNew(
                userId: user.Id,
                sessionId: sessionId,
                familyId: stored.FamilyId,
                parentTokenId: stored.Id,
                rawRefreshToken: tokenData.RefreshToken,
                expiresAt: tokenData.RefreshTokenExpiration,
                requestContext: _requestContext);

            _refreshTokenService.Revoke(stored, replacedByTokenId: newRefresh.Id, reason: "rotated", revokedByIp: _requestContext.IpAddress);

            user.LastLoginAt = DateTime.UtcNow;

            await _unitOfWork.RefreshTokens.AddAsync(newRefresh);
            await _unitOfWork.RefreshTokens.UpdateAsync(stored);
            await _unitOfWork.Users.UpdateAsync(user);

            var dbSession = await _unitOfWork.UserSessions.GetBySessionIdAsync(sessionId, disableTracking: false);
            if (dbSession is not null && dbSession.RevokedAt is null)
            {
                dbSession.CurrentRefreshTokenId = newRefresh.Id;
                dbSession.LastActivityAt = DateTime.UtcNow;
                await _unitOfWork.UserSessions.UpdateAsync(dbSession);
            }

            await _unitOfWork.SaveChangesAsync();
            await _sessionCacheService.UpdateLastActivityAsync(sessionId, CancellationToken.None);

            _logger.LogAuthEvent("RefreshToken", user.PhoneNumber, true, additionalProperties: new Dictionary<string, object>
            {
                { "UserId", user.Id },
                { "SessionId", sessionId }
            });

            var response = new LoginDto
            {
                TokenModel = tokenData,
                User = user.Adapt<UserDto>()
            };

            return result.Succeed(IdentityMessages.OperationSucceeded, response);
        }
        catch (Exception ex)
        {
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> LogoutCurrentSessionAsync(CancellationToken cancellationToken = default)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            var userId = _currentUser.UserId ?? Guid.Empty;
            var sessionId = _currentUser.SessionId ?? Guid.Empty;
            if (userId == Guid.Empty || sessionId == Guid.Empty)
                return result.Failed(IdentityMessages.InvalidSessionIdentifiers, HttpStatusCode.BadRequest);

            var dbSession = await _unitOfWork.UserSessions.GetBySessionIdAsync(sessionId, disableTracking: false);
            if (dbSession is not null && dbSession.UserId == userId && dbSession.RevokedAt is null)
            {
                dbSession.RevokedAt = DateTime.UtcNow;
                dbSession.LastActivityAt = DateTime.UtcNow;
                await _unitOfWork.UserSessions.UpdateAsync(dbSession);
            }

            var tokens = await _unitOfWork.RefreshTokens.GetAllAsync(t => t.UserId == userId && t.SessionId == sessionId && t.RevokedAt == null, disableTracking: false);
            foreach (var token in tokens)
            {
                _refreshTokenService.Revoke(token, replacedByTokenId: null, reason: "logout", revokedByIp: _requestContext.IpAddress);
                await _unitOfWork.RefreshTokens.UpdateAsync(token);
            }

            await _unitOfWork.SaveChangesAsync();
            await _sessionCacheService.RevokeSessionAsync(sessionId, CancellationToken.None);

            return result.Succeed(IdentityMessages.LogoutSucceeded);
        }
        catch (Exception ex)
        {
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> RevokeSessionAsync(RevokeSessionDto dto, CancellationToken cancellationToken = default)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            var userId = _currentUser.UserId ?? Guid.Empty;
            var sessionId = dto.SessionId;
            if (userId == Guid.Empty)
                return result.Failed(IdentityMessages.AuthenticationRequired, HttpStatusCode.Unauthorized);

            var dbSession = await _unitOfWork.UserSessions.GetBySessionIdAsync(sessionId, disableTracking: false);
            if (dbSession is null || dbSession.RevokedAt is not null)
                return result.Succeed(IdentityMessages.OperationSucceeded);

            if (dbSession.UserId != userId)
                return result.Failed(IdentityMessages.SessionAccessDenied, HttpStatusCode.Forbidden);

            dbSession.RevokedAt = DateTime.UtcNow;
            dbSession.LastActivityAt = DateTime.UtcNow;
            await _unitOfWork.UserSessions.UpdateAsync(dbSession);

            var tokens = await _unitOfWork.RefreshTokens.GetAllAsync(t => t.UserId == dbSession.UserId && t.SessionId == dbSession.SessionId && t.RevokedAt == null, disableTracking: false);
            foreach (var token in tokens)
            {
                _refreshTokenService.Revoke(token, replacedByTokenId: null, reason: "session_revoked", revokedByIp: _requestContext.IpAddress);
                await _unitOfWork.RefreshTokens.UpdateAsync(token);
            }

            await _unitOfWork.SaveChangesAsync();
            await _sessionCacheService.RevokeSessionAsync(dbSession.SessionId, CancellationToken.None);

            return result.Succeed(IdentityMessages.OperationSucceeded);
        }
        catch (Exception ex)
        {
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> RevokeAllSessionsAsync(CancellationToken cancellationToken = default)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            var userId = _currentUser.UserId ?? Guid.Empty;
            if (userId == Guid.Empty)
                return result.Failed(IdentityMessages.AuthenticationRequired, HttpStatusCode.Unauthorized);

            var sessions = await _unitOfWork.UserSessions.GetAllAsync(s => s.UserId == userId && s.RevokedAt == null, disableTracking: false);
            foreach (var session in sessions)
            {
                session.RevokedAt = DateTime.UtcNow;
                session.LastActivityAt = DateTime.UtcNow;
                await _unitOfWork.UserSessions.UpdateAsync(session);
            }

            var tokens = await _unitOfWork.RefreshTokens.GetAllAsync(t => t.UserId == userId && t.RevokedAt == null, disableTracking: false);
            foreach (var token in tokens)
            {
                _refreshTokenService.Revoke(token, replacedByTokenId: null, reason: "revoke_all_sessions", revokedByIp: _requestContext.IpAddress);
                await _unitOfWork.RefreshTokens.UpdateAsync(token);
            }

            await _unitOfWork.SaveChangesAsync();
            await _sessionCacheService.RevokeAllSessionsAsync(userId, CancellationToken.None);

            return result.Succeed(IdentityMessages.OperationSucceeded);
        }
        catch (Exception ex)
        {
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<SessionDto>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        var result = new ApiOperationResult<SessionDto>();
        try
        {
            var userId = _currentUser.UserId ?? Guid.Empty;
            if (userId == Guid.Empty)
                return result.Failed(IdentityMessages.AuthenticationRequired, HttpStatusCode.Unauthorized);

            var sessions = await _unitOfWork.UserSessions.GetActiveByUserAsync(userId);
            var list = sessions.Select(s =>
            {
                var dto = s.Adapt<SessionDto>();
                dto.IsActive = s.RevokedAt is null;
                return dto;
            }).ToList();

            return result.Succeed(IdentityMessages.OperationSucceeded, list, list.Count);
        }
        catch (Exception ex)
        {
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> CreateAsync(CreateUserDto dto)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            var validation = await _createUserValidator.ValidateAsync(dto, CancellationToken.None);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(error => error.ErrorMessage).ToList();
                return result.Failed(IdentityMessages.ValidationFailed, errors, HttpStatusCode.BadRequest);
            }

            var existPhone = await _unitOfWork.Users.GetAsync(user => user.PhoneNumber == dto.PhoneNumber);
            if (existPhone is not null)
                return result.Failed(IdentityMessages.PhoneAlreadyRegistered, HttpStatusCode.Conflict);

            var existEmail = await _unitOfWork.Users.GetAsync(user => user.Email == dto.Email);
            if (existEmail is not null)
                return result.Failed(IdentityMessages.EmailAlreadyRegistered, HttpStatusCode.Conflict);

            var existNationalCode = string.IsNullOrWhiteSpace(dto.NationalCode)
                ? null
                : await _unitOfWork.Users.GetAsync(user => user.NationalCode == dto.NationalCode);
            if (existNationalCode is not null)
                return result.Failed(IdentityMessages.NationalCodeAlreadyRegistered, HttpStatusCode.Conflict);

            var user = dto.Adapt<User>();
            user.CreatedAt = DateTime.UtcNow;
            user.Role = _otpOptions.Value.DevBypassEnabled
                && !string.IsNullOrWhiteSpace(_otpOptions.Value.SeedAdminPhone)
                && string.Equals(dto.PhoneNumber, _otpOptions.Value.SeedAdminPhone, StringComparison.Ordinal)
                ? UserRole.Admin
                : UserRole.User;

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return result.Succeed(IdentityMessages.UserCreated, user.Adapt<UserDto>());
        }
        catch (Exception ex)
        {
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            var requesterId = _currentUser.UserId ?? Guid.Empty;
            if (requesterId == Guid.Empty)
                return result.Failed(IdentityMessages.AuthenticationRequired, HttpStatusCode.Unauthorized);

            var validation = await _updateUserValidator.ValidateAsync(dto, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return result.Failed(IdentityMessages.ValidationFailed, errors, HttpStatusCode.BadRequest);
            }

            var user = await _unitOfWork.Users.GetAsync(u => u.Id == id && !u.IsDeleted, false);
            if (user == null) return result.Failed(IdentityMessages.UserNotFound, HttpStatusCode.NotFound);

            var requester = await _unitOfWork.Users.GetAsync(u => u.Id == requesterId && !u.IsDeleted, false);
            if (dto.Role.HasValue && user.Role != dto.Role.Value && requester != null && requester.Role != UserRole.Admin)
            {
                return result.Failed(IdentityMessages.OnlyAdminCanChangeRole, HttpStatusCode.BadRequest);
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && dto.PhoneNumber != user.PhoneNumber)
            {
                var phoneExists = await _unitOfWork.Users.GetAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != id);
                if (phoneExists != null) return result.Failed(IdentityMessages.PhoneAlreadyRegistered, HttpStatusCode.Conflict);
                user.PhoneNumber = dto.PhoneNumber;
            }

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
            {
                var emailExists = await _unitOfWork.Users.GetAsync(u => u.Email == dto.Email && u.Id != id);
                if (emailExists != null) return result.Failed(IdentityMessages.EmailAlreadyRegistered, HttpStatusCode.Conflict);
                user.Email = dto.Email;
            }

            if (!string.IsNullOrWhiteSpace(dto.NationalCode) && dto.NationalCode != user.NationalCode)
            {
                var nationalCodeExists = await _unitOfWork.Users.GetAsync(u => u.NationalCode == dto.NationalCode && u.Id != id);
                if (nationalCodeExists != null) return result.Failed(IdentityMessages.NationalCodeAlreadyRegistered, HttpStatusCode.Conflict);
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

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return result.Succeed(IdentityMessages.UserUpdated, user.Adapt<UserDto>());
        }
        catch (Exception ex)
        {
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> GetByIdAsync(Guid id)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            var user = await _unitOfWork.Users.GetAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null) return result.Failed(IdentityMessages.UserNotFound, HttpStatusCode.NotFound);
            return result.Succeed(IdentityMessages.OperationSucceeded, user.Adapt<UserDto>());
        }
        catch (Exception ex)
        {
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> GetPagedAsync(int take, int skip)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            var users = await _unitOfWork.Users.GetPagedListAsync(take, skip, u => !u.IsDeleted);
            var total = await _unitOfWork.Users.CountAsync(u => !u.IsDeleted);
            return result.Succeed(IdentityMessages.OperationSucceeded, users.Select(u => u.Adapt<UserDto>()).ToList(), total);
        }
        catch (Exception ex)
        {
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> DeleteAsync(Guid id)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            var user = await _unitOfWork.Users.GetAsync(u => u.Id == id && !u.IsDeleted, false);
            if (user == null) return result.Failed(IdentityMessages.UserNotFound, HttpStatusCode.NotFound);

            await _unitOfWork.Users.DeleteAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return result.Succeed(IdentityMessages.UserDeleted);
        }
        catch (Exception ex)
        {
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiOperationResult<UserDto>> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var result = new ApiOperationResult<UserDto>();
        try
        {
            var userId = _currentUser.UserId ?? Guid.Empty;
            if (userId == Guid.Empty)
                return result.Failed(IdentityMessages.AuthenticationRequired, HttpStatusCode.Unauthorized);

            var user = await _unitOfWork.Users.GetAsync(u => u.Id == userId && !u.IsDeleted, false);
            return result.Succeed(IdentityMessages.OperationSucceeded, user is null ? new UserDto() : user.Adapt<UserDto>());
        }
        catch (Exception ex)
        {
            return result.Failed($"{IdentityMessages.InternalError}: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    private ApiOperationResult<LoginDto> BuildOtpFailure(
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
            IpAddress: _requestContext.IpAddress,
            UserAgent: _requestContext.UserAgent,
            CreatedAt: now,
            ExpiresAt: now.AddDays(Math.Max(1, _sessionOptions.Value.AbsoluteExpirationDays)));
    }

    private UserSession CreateUserSession(Guid userId, Guid sessionId, Guid refreshTokenId)
    {
        var now = DateTime.UtcNow;
        return new UserSession
        {
            SessionId = sessionId,
            UserId = userId,
            CurrentRefreshTokenId = refreshTokenId,
            DeviceId = _requestContext.DeviceId,
            UserAgent = _requestContext.UserAgent,
            IpAddress = _requestContext.IpAddress,
            CreatedAt = now,
            LastActivityAt = now
        };
    }

    private async Task InvalidateFamilyAsync(Guid userId, Guid familyId, string reason, CancellationToken cancellationToken)
    {
        var tokens = await _unitOfWork.RefreshTokens.GetAllAsync(t => t.UserId == userId && t.FamilyId == familyId, disableTracking: false);
        foreach (var token in tokens)
        {
            if (token.RevokedAt is null)
            {
                _refreshTokenService.Revoke(token, replacedByTokenId: null, reason: reason, revokedByIp: _requestContext.IpAddress);
                await _unitOfWork.RefreshTokens.UpdateAsync(token);
            }
        }

        await _sessionCacheService.RevokeAllSessionsAsync(userId, cancellationToken);

        var sessions = await _unitOfWork.UserSessions.GetAllAsync(s => s.UserId == userId && s.RevokedAt == null, disableTracking: false);
        foreach (var session in sessions)
        {
            session.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.UserSessions.UpdateAsync(session);
        }
    }
}