using Core.Application.Identity.Tokens;


namespace Core.Application.Identity.Abstractions;

public interface ITokenHelper
{
    GeneratedTokenModel GenerateToken(string userId, string username, string role);
    GeneratedTokenModel GenerateToken(string userId, string username, string role, string sessionId);
    AccessTokenPayload ValidateAccessToken(string accessToken);
}