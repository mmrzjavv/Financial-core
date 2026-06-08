using Core.Application.DTOs;
using Core.Domain.Enums;
using Core.Domain.Identity;

namespace Core.Application.Common;

public static class FundCreditLimitAuthorization
{
    private static readonly HashSet<string> AuthorizedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        UserRoleClaims.Ceo,
        UserRoleClaims.TechnicalExpert,
        UserRoleClaims.Admin,
        "CEO"
    };

    public static bool CanAccessFundCreditLimits(IEnumerable<string> roles)
        => roles.Any(r => AuthorizedRoles.Contains(r));
}
