using System.ComponentModel.DataAnnotations.Schema;

namespace Services.CoreService.Core.Domain.Identity.Entities;

[Table("RefreshTokens", Schema = "Identity")]
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public Guid FamilyId { get; set; }
    public Guid? ParentTokenId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string? CreatedByIp { get; set; }
    public string? CreatedByUserAgent { get; set; }
    public string? DeviceId { get; set; }
    public string? RevokedByIp { get; set; }
    public string? RevocationReason { get; set; }
}
