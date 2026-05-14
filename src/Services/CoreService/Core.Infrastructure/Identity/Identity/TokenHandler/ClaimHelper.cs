using System.Security.Claims;
using Core.Infrastructure.Identity.Identity.TokenHandler.Model;
using Newtonsoft.Json;
using static Core.Infrastructure.Identity.Identity.TokenHandler.Model.UserDataClaim;
namespace Core.Infrastructure.Identity.Identity.TokenHandler
{
    public static class ClaimHelper
    {
        public static UserDataClaim.UserTokenData GetClaimData(this IEnumerable<Claim>? claims)
        {
            if (claims == null || !claims.Any())
            {
                throw new ArgumentException("Claims collection is null or empty.");
            }
            var userDataClaim = claims.FirstOrDefault(x => x.Type == ClaimTypes.UserData)?.Value;
            if (userDataClaim == null)
            {
                throw new InvalidOperationException("User data claim not found.");
            }
            var userData = JsonConvert.DeserializeObject<UserDataClaim.UserData>(userDataClaim);
            return new UserDataClaim.UserTokenData
            {
                Username = claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value ?? string.Empty,
                Id = Guid.TryParse(claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : Guid.Empty,
                RoleId = Guid.TryParse(claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value, out var roleId) ? roleId : Guid.Empty,
                Name = userData?.Name ?? string.Empty,
                LastName = userData?.LastName ?? string.Empty,
                MobileNo = userData?.MobileNo ?? string.Empty
            };
        }
    }
}