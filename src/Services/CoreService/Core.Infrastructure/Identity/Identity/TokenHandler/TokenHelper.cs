using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Core.Application.Identity.Abstractions;
using Core.Application.Identity.Tokens;
using Core.Infrastructure.Identity.Identity.TokenHandler.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Core.Infrastructure.Identity.Identity.TokenHandler;

public sealed class TokenHelper(IConfiguration configuration) : ITokenHelper
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private const int TokenExpirationHours = 6;
    private const int RefreshTokenExpirationDays = 15;
    private const int RefreshTokenSize = 32;

    public GeneratedTokenModel GenerateToken(string userId, string username, string role)
    {
        return GenerateToken(userId, username, role, sessionId: string.Empty);
    }

    public GeneratedTokenModel GenerateToken(string userId, string username, string role, string sessionId)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrWhiteSpace(username))
        {
            return new GeneratedTokenModel();
        }

        var now = DateTime.UtcNow;
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenKey = Encoding.UTF8.GetBytes(_configuration["JwtKey"] ?? "");
        var encryptionKey = Encoding.UTF8.GetBytes(_configuration["ENCKey"] ?? "");
        var accessTokenExpiration = now.AddHours(TokenExpirationHours);
        var refreshTokenExpiration = now.AddDays(RefreshTokenExpirationDays);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role),
            // SessionId claim for multi-session tracking (Phase 3). Empty means "legacy/no session".
            new Claim("sid", sessionId ?? string.Empty),
            new Claim(ClaimTypes.UserData, JsonConvert.SerializeObject(new UserDataClaim.UserData
            {
                Name = username,
                LastName = string.Empty,
                MobileNo = string.Empty,
            }))
        };
        var securityKey = new SymmetricSecurityKey(tokenKey);
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
        var encryptingKey = new SymmetricSecurityKey(encryptionKey);
        var encryptingCredentials = new EncryptingCredentials(encryptingKey, SecurityAlgorithms.Aes128KW,
            SecurityAlgorithms.Aes128CbcHmacSha256);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            Expires = accessTokenExpiration,
            SigningCredentials = signingCredentials,
            EncryptingCredentials = encryptingCredentials,
            CompressionAlgorithm = CompressionAlgorithms.Deflate,
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var refreshToken = GenerateRefreshToken();
        var accessToken = tokenHandler.WriteToken(token);
        return new GeneratedTokenModel
        {
            AccessToken = accessToken,
            AccessTokenExpiration = accessTokenExpiration,
            RefreshToken = refreshToken,
            RefreshTokenExpiration = refreshTokenExpiration
        };
    }

    public AccessTokenPayload ValidateAccessToken(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return new AccessTokenPayload();
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenKey = Encoding.UTF8.GetBytes(_configuration["JwtKey"] ?? "");
        var encryptionKey = Encoding.UTF8.GetBytes(_configuration["ENCKey"] ?? "");
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(tokenKey),
                TokenDecryptionKey = new SymmetricSecurityKey(encryptionKey),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };
            var principal =
                tokenHandler.ValidateToken(accessToken, validationParameters, out SecurityToken validatedToken);
            var jwtToken = validatedToken as JwtSecurityToken;
            if (jwtToken?.Claims == null)
            {
                return new AccessTokenPayload();
            }

            var userIdClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "nameid");
            var sessionIdClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "sid");
            var expirationClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "exp");
            if (userIdClaim == null || expirationClaim == null)
            {
                return new AccessTokenPayload();
            }

            var userId = Guid.Parse(userIdClaim.Value);
            var sessionId = Guid.TryParse(sessionIdClaim?.Value, out var sid) ? sid : Guid.Empty;
            var expiration = long.Parse(expirationClaim.Value);
            return new AccessTokenPayload
            {
                UserId = userId,
                SessionId = sessionId,
                Expiration = DateTimeOffset.FromUnixTimeSeconds(expiration).UtcDateTime
            };
        }
        catch (Exception)
        {
            return new AccessTokenPayload();
        }
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[RefreshTokenSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    // For refresh rotation handlers.
    public static string GenerateRefreshTokenValue()
    {
        return GenerateRefreshToken();
    }
}
