using Core.Domain.Identity;

namespace Core.Application.Identity.Authorization;

public static class RoleClaimMapper
{
    public static string ToClaimRole(UserRole role) => UserRoleClaims.From(role);
}
