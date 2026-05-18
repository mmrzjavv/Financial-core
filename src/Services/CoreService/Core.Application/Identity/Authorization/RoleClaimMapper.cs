using Core.Domain.Constants;
using Core.Domain.Identity.Enums;

namespace Core.Application.Identity.Authorization;

public static class RoleClaimMapper
{
    public static string ToClaimRole(UserRole role) =>
        role switch
        {
            UserRole.User => SystemRoles.Applicant,
            UserRole.LegalUnit => SystemRoles.LegalExpert,
            UserRole.FinancialUnit => SystemRoles.FinancialExpert,
            _ => role.ToString()
        };
}