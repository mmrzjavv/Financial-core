using System.Security.Cryptography;
using System.Text;
using Core.Application.Identity.Common.Interfaces;
using Core.Domain.Identity.Entities;
using Microsoft.Extensions.Configuration;

namespace Core.Infrastructure.Identity.Identity;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly string _pepper;

    public RefreshTokenService(IConfiguration configuration)
    {
        _pepper = configuration["RefreshTokenPepper"] ?? string.Empty;
    }

    public string Hash(string refreshToken)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes($"{_pepper}:{refreshToken}");
        return Convert.ToBase64String(sha.ComputeHash(bytes));
    }

    public RefreshToken CreateNew(Guid userId, Guid sessionId, Guid familyId, Guid? parentTokenId, string rawRefreshToken, DateTime expiresAt, ICurrentRequestContext requestContext)
    {
        return new RefreshToken
        {
            UserId = userId,
            SessionId = sessionId,
            FamilyId = familyId,
            ParentTokenId = parentTokenId,
            TokenHash = Hash(rawRefreshToken),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = requestContext.IpAddress,
            CreatedByUserAgent = requestContext.UserAgent,
            DeviceId = requestContext.DeviceId
        };
    }

    public void Revoke(RefreshToken token, Guid? replacedByTokenId, string? reason, string? revokedByIp)
    {
        token.RevokedAt = DateTime.UtcNow;
        token.ReplacedByTokenId = replacedByTokenId;
        token.RevocationReason = reason;
        token.RevokedByIp = revokedByIp;
    }
}
