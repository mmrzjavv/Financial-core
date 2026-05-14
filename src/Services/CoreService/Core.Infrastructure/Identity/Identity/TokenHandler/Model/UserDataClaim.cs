namespace Core.Infrastructure.Identity.Identity.TokenHandler.Model;
public class UserDataClaim
{
    public class UserTokenData
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
        public string Name { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string MobileNo { get; init; } = string.Empty;
    }
    public class UserData
    {
        public string Name { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string MobileNo { get; init; } = string.Empty;
    }
}