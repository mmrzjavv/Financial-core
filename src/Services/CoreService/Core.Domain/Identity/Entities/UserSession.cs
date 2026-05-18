using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Identity.Entities;

[Table("UserSessions", Schema = "Identity")]
public class UserSession : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public Guid? CurrentRefreshTokenId { get; set; }
    public string? DeviceId { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    [NotMapped]
    public bool IsActive => RevokedAt is null;
}

