using System.Security.Claims;
using Asp.Versioning;
using Core.Application.Identity.Common.Interfaces;
using Core.Application.Identity.DTOs.User;
using Core.Application.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/panel/users")]
public sealed class UserController(IUserService userService, ILoggingService logger) : ControllerBase
{
    private Guid GetUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
    }

    [HttpPost("send-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpDto dto)
    {
        var start = DateTime.UtcNow;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await userService.SendOtpAsync(dto);

        logger.LogSecurityEvent("anonymous", "SendOtp", ipAddress,
            details: $"Phone={dto.PhoneNumber}");
        logger.LogPerformanceMetric("User.SendOtp", DateTime.UtcNow - start);

        return StatusCode((int)result.Status, result);
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        var start = DateTime.UtcNow;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await userService.VerifyOtpAsync(dto);

        logger.LogSecurityEvent("anonymous", "VerifyOtp", ipAddress,
            details: $"Phone={dto.PhoneNumber}");
        logger.LogPerformanceMetric("User.VerifyOtp", DateTime.UtcNow - start);

        return StatusCode((int)result.Status, result);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        var start = DateTime.UtcNow;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var bearer = Request.Headers.Authorization.ToString();
        var accessToken = bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? bearer["Bearer ".Length..].Trim()
            : bearer.Trim();

        var result = await userService.RefreshTokenAsync(dto, accessToken);

        logger.LogSecurityEvent("anonymous", "RefreshToken", ipAddress);
        logger.LogPerformanceMetric("User.RefreshToken", DateTime.UtcNow - start);

        return StatusCode((int)result.Status, result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = GetUserId();
        var start = DateTime.UtcNow;

        var sid = User.FindFirstValue("sid");
        var sessionId = Guid.TryParse(sid, out var parsed) ? parsed : Guid.Empty;

        var result = await userService.LogoutCurrentSessionAsync(userId, sessionId);

        logger.LogUserActivity(userId.ToString(), "Logout", "Session", sessionId == Guid.Empty ? null : sessionId.ToString());
        logger.LogPerformanceMetric("User.Logout", DateTime.UtcNow - start);

        return StatusCode((int)result.Status, result);
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetActiveSessions()
    {
        var userId = GetUserId();
        var start = DateTime.UtcNow;

        var result = await userService.GetActiveSessionsAsync(userId);

        logger.LogDataAccess(userId.ToString(), "GetActiveSessions", "Session");
        logger.LogPerformanceMetric("User.GetActiveSessions", DateTime.UtcNow - start);

        return StatusCode((int)result.Status, result);
    }

    [HttpPost("sessions/revoke")]
    [Authorize]
    public async Task<IActionResult> RevokeSession([FromBody] RevokeSessionDto dto)
    {
        var userId = GetUserId();
        var start = DateTime.UtcNow;

        var result = await userService.RevokeSessionAsync(userId, dto.SessionId);

        logger.LogSecurityEvent(userId.ToString(), "RevokeSession", HttpContext.Connection.RemoteIpAddress?.ToString(), $"SessionId={dto.SessionId}");
        logger.LogPerformanceMetric("User.RevokeSession", DateTime.UtcNow - start);

        return StatusCode((int)result.Status, result);
    }

    [HttpPost("sessions/revoke-all")]
    [Authorize]
    public async Task<IActionResult> RevokeAllSessions()
    {
        var userId = GetUserId();
        var start = DateTime.UtcNow;

        var result = await userService.RevokeAllSessionsAsync(userId);

        logger.LogSecurityEvent(userId.ToString(), "RevokeAllSessions", HttpContext.Connection.RemoteIpAddress?.ToString());
        logger.LogPerformanceMetric("User.RevokeAllSessions", DateTime.UtcNow - start);

        return StatusCode((int)result.Status, result);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        var start = DateTime.UtcNow;

        var result = await userService.GetByIdAsync(id);

        logger.LogDataAccess(userId.ToString(), "GetById", "User", id.ToString());
        logger.LogPerformanceMetric("User.GetById", DateTime.UtcNow - start, $"TargetUserId={id}");

        return StatusCode((int)result.Status, result);
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = GetUserId();
        var start = DateTime.UtcNow;

        var result = await userService.Profile(userId);

        logger.LogUserActivity(userId.ToString(), "ViewProfile", "User", userId.ToString());
        logger.LogPerformanceMetric("User.Profile", DateTime.UtcNow - start);

        return StatusCode((int)result.Status, result);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetPaged([FromQuery] int take = 10, [FromQuery] int skip = 0)
    {
        var userId = GetUserId();
        var start = DateTime.UtcNow;

        var result = await userService.GetPagedAsync(take, skip);

        logger.LogDataAccess(userId.ToString(), "GetPagedUsers", "User",
            details: $"take={take}, skip={skip}");
        logger.LogPerformanceMetric("User.GetPaged", DateTime.UtcNow - start);

        return StatusCode((int)result.Status, result);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var start = DateTime.UtcNow;

        var result = await userService.CreateAsync(dto);

        logger.LogSystemOperation("User.Create", $"Phone={dto.PhoneNumber}");
        logger.LogPerformanceMetric("User.Create", DateTime.UtcNow - start);

        return StatusCode((int)result.Status, result);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        var userId = GetUserId();
        var start = DateTime.UtcNow;

        var result = await userService.UpdateAsync(id, userId, dto);

        logger.LogUserActivity(userId.ToString(), "Update", "User", id.ToString());
        logger.LogPerformanceMetric("User.Update", DateTime.UtcNow - start, $"TargetUserId={id}");

        return StatusCode((int)result.Status, result);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var start = DateTime.UtcNow;

        var result = await userService.DeleteAsync(id);

        logger.LogUserActivity(userId.ToString(), "Delete", "User", id.ToString());
        logger.LogPerformanceMetric("User.Delete", DateTime.UtcNow - start, $"TargetUserId={id}");

        return StatusCode((int)result.Status, result);
    }
}
