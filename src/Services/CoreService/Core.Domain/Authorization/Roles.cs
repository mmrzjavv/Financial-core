using Services.CoreService.Core.Domain.Constants;

namespace Services.CoreService.Core.Domain.Authorization;

public static class Roles
{
    public const string Applicant = SystemRoles.Applicant;
    public const string InvestmentExpert = SystemRoles.InvestmentExpert;
    public const string InvestmentManager = SystemRoles.InvestmentManager;
    public const string LegalExpert = SystemRoles.LegalExpert;
    public const string FinancialExpert = SystemRoles.FinancialExpert;
    public const string Admin = SystemRoles.Admin;
}

