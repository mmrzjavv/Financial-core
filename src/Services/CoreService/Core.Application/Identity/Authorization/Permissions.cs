using Core.Domain.Identity;

namespace Core.Application.Identity.Authorization;

public static class Permissions
{
    public const string Users_Read = "users:read";
    public const string Users_Write = "users:write";
    public const string Users_Delete = "users:delete";
    public const string Users_ManageRoles = "users:manage_roles";

    public const string Companies_Read = "companies:read";
    public const string Companies_Write = "companies:write";
    public const string Companies_Delete = "companies:delete";

    public const string Sessions_Read = "sessions:read";
    public const string Sessions_Write = "sessions:write";
    public const string Sessions_Revoke = "sessions:revoke";

    public const string Otp_Send = "otp:send";
    public const string Otp_Verify = "otp:verify";

    public const string Admin_FullAccess = "admin:full_access";

    public const string InvestmentCases_Read = "investment_cases:read";
    public const string InvestmentCases_Write = "investment_cases:write";
    public const string InvestmentCases_Review = "investment_cases:review";
    public const string InvestmentCases_FinanceReview = "investment_cases:finance_review";
    public const string InvestmentCases_LegalReview = "investment_cases:legal_review";
    public const string InvestmentCases_CeoApprove = "investment_cases:ceo_approve";
}

public static class RolePermissions
{
    /// <summary>Every API permission string — assigned to <see cref="UserRoleClaims.Admin"/>.</summary>
    public static readonly IReadOnlyCollection<string> AllPermissions =
    [
        Permissions.Users_Read,
        Permissions.Users_Write,
        Permissions.Users_Delete,
        Permissions.Users_ManageRoles,
        Permissions.Companies_Read,
        Permissions.Companies_Write,
        Permissions.Companies_Delete,
        Permissions.Sessions_Read,
        Permissions.Sessions_Write,
        Permissions.Sessions_Revoke,
        Permissions.Otp_Send,
        Permissions.Otp_Verify,
        Permissions.Admin_FullAccess,
        Permissions.InvestmentCases_Read,
        Permissions.InvestmentCases_Write,
        Permissions.InvestmentCases_Review,
        Permissions.InvestmentCases_FinanceReview,
        Permissions.InvestmentCases_LegalReview,
        Permissions.InvestmentCases_CeoApprove
    ];

    private static readonly string[] LegalUnitPermissions =
    [
        Permissions.Users_Read,
        Permissions.Companies_Read,
        Permissions.Sessions_Read,
        Permissions.InvestmentCases_Read,
        Permissions.InvestmentCases_LegalReview
    ];

    private static readonly string[] FinancialUnitPermissions =
    [
        Permissions.Users_Read,
        Permissions.Companies_Read,
        Permissions.Companies_Write,
        Permissions.Sessions_Read,
        Permissions.InvestmentCases_Read,
        Permissions.InvestmentCases_FinanceReview
    ];

    private static readonly string[] TechnicalUnitPermissions =
    [
        Permissions.Users_Read,
        Permissions.Sessions_Read,
        Permissions.Otp_Send,
        Permissions.Otp_Verify,
        Permissions.InvestmentCases_Read,
        Permissions.InvestmentCases_Review
    ];

    public static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> RolePermissionMappings =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [UserRoleClaims.Admin] = AllPermissions,
            [UserRoleClaims.Applicant] =
            [
                Permissions.Users_Read,
                Permissions.Companies_Read,
                Permissions.Companies_Write,
                Permissions.Sessions_Read,
                Permissions.Otp_Send,
                Permissions.Otp_Verify
            ],
            [UserRoleClaims.InvestmentExpert] =
            [
                Permissions.Users_Read,
                Permissions.Companies_Read,
                Permissions.Companies_Write,
                Permissions.Sessions_Read,
                Permissions.Otp_Send,
                Permissions.Otp_Verify,
                Permissions.InvestmentCases_Read,
                Permissions.InvestmentCases_Review
            ],
            [UserRoleClaims.InvestmentManager] =
            [
                Permissions.Users_Read,
                Permissions.Users_Write,
                Permissions.Companies_Read,
                Permissions.Companies_Write,
                Permissions.Sessions_Read,
                Permissions.Otp_Send,
                Permissions.Otp_Verify,
                Permissions.InvestmentCases_Read,
                Permissions.InvestmentCases_Review,
                Permissions.InvestmentCases_Write
            ],
            [UserRoleClaims.Ceo] =
            [
                Permissions.Users_Read,
                Permissions.Companies_Read,
                Permissions.Sessions_Read,
                Permissions.InvestmentCases_Read,
                Permissions.InvestmentCases_CeoApprove
            ],
            [UserRoleClaims.LegalExpert] = LegalUnitPermissions,
            [UserRoleClaims.LegalManager] = LegalUnitPermissions,
            [UserRoleClaims.LegalUnit] = LegalUnitPermissions,
            [UserRoleClaims.FinancialExpert] = FinancialUnitPermissions,
            [UserRoleClaims.FinancialManager] = FinancialUnitPermissions,
            [UserRoleClaims.FinancialUnit] = FinancialUnitPermissions,
            [UserRoleClaims.TechnicalExpert] = TechnicalUnitPermissions,
            [UserRoleClaims.TechnicalManager] = TechnicalUnitPermissions
        };
}
