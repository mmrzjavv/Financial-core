using Core.API.Authorization;
using Core.Domain.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Core.API.DependencyInjection;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddCoreAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", p => p.RequireRole(UserRoleClaims.Admin));
            options.AddPolicy("ApplicantOnly", p => p.RequireRole(UserRoleClaims.Applicant, UserRoleClaims.Admin));
            options.AddPolicy("InternalOnly", p => p.RequireRole(
                UserRoleClaims.InvestmentExpert,
                UserRoleClaims.InvestmentManager,
                UserRoleClaims.CreditExpert,
                UserRoleClaims.CreditManager,
                UserRoleClaims.LegalExpert,
                UserRoleClaims.LegalManager,
                UserRoleClaims.FinancialExpert,
                UserRoleClaims.FinancialManager,
                UserRoleClaims.TechnicalExpert,
                UserRoleClaims.TechnicalManager,
                UserRoleClaims.Ceo,
                UserRoleClaims.Admin,
                UserRoleClaims.LegalUnit,
                UserRoleClaims.FinancialUnit,
                UserRoleClaims.InvestmentUnit,
                "CEO"));

            options.AddPolicy("GuaranteeCases.CreditReview", p => p.Requirements.Add(new PermissionRequirement("guarantee_cases:credit_review")));
            options.AddPolicy("GuaranteeCases.CeoApprove", p => p.RequireRole(
                UserRoleClaims.Ceo,
                UserRoleClaims.Admin,
                "CEO"));

            options.AddPolicy("GuaranteeCases.CeoOnly", p => p.RequireRole(UserRoleClaims.Ceo, "CEO"));

            options.AddPolicy("LoanCases.CeoApprove", p => p.RequireRole(
                UserRoleClaims.Ceo,
                UserRoleClaims.Admin,
                "CEO"));

            options.AddPolicy("InvestmentCases.Review", p => p.Requirements.Add(new PermissionRequirement("investment_cases:review")));
            options.AddPolicy("InvestmentCases.FinanceReview", p => p.Requirements.Add(new PermissionRequirement("investment_cases:finance_review")));
            options.AddPolicy("InvestmentCases.LegalReview", p => p.Requirements.Add(new PermissionRequirement("investment_cases:legal_review")));
            options.AddPolicy("InvestmentCases.CeoApprove", p => p.RequireRole(
                UserRoleClaims.Ceo,
                UserRoleClaims.Admin,
                "CEO"));

            options.AddPolicy("Dashboard.Ceo", p => p.RequireRole(UserRoleClaims.Ceo, UserRoleClaims.Admin, "CEO"));
            options.AddPolicy("Dashboard.Board", p => p.RequireRole(
                UserRoleClaims.Ceo,
                UserRoleClaims.InvestmentManager,
                UserRoleClaims.Admin,
                "CEO"));
        });

        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
