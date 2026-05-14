namespace Core.Infrastructure.Identity.Identity.TokenHandler.Model
{
    public class TokenModel
    {
        public string Name { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
    }
}