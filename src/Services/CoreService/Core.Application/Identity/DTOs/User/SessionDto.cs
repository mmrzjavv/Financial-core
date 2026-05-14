namespace Core.Application.Identity.DTOs.User;

public class SessionDto
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public Guid? CurrentRefreshTokenId { get; set; }
    public string? DeviceId { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsActive { get; set; }
}

