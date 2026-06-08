using Core.Domain.Identity;

namespace Core.Application.Identity.DTOs.User;

public class OnlineUserDto
{
    public Guid UserId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int ActiveSessionCount { get; set; }
    public DateTime LastActivityAt { get; set; }
    public string? LatestIpAddress { get; set; }
    public string? LatestUserAgent { get; set; }
    public List<SessionDto> Sessions { get; set; } = [];
}
