using Core.Domain.Identity.Entities;

namespace Core.Application.Identity.Common.Interfaces;

public interface IRefreshTokenService
{
    string Hash(string refreshToken);
    RefreshToken CreateNew(Guid userId, Guid sessionId, Guid familyId, Guid? parentTokenId, string rawRefreshToken, DateTime expiresAt, ICurrentRequestContext requestContext);
    void Revoke(RefreshToken token, Guid? replacedByTokenId, string? reason, string? revokedByIp);
}