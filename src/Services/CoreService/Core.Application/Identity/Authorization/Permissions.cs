namespace Core.Application.Identity.Authorization;

public static class Permissions
{
    // User management permissions
    public const string Users_Read = "users:read";
    public const string Users_Write = "users:write";
    public const string Users_Delete = "users:delete";
    public const string Users_ManageRoles = "users:manage_roles";

    // Company management permissions
    public const string Companies_Read = "companies:read";
    public const string Companies_Write = "companies:write";
    public const string Companies_Delete = "companies:delete";

    // Session management permissions
    public const string Sessions_Read = "sessions:read";
    public const string Sessions_Write = "sessions:write";
    public const string Sessions_Revoke = "sessions:revoke";

    // OTP management permissions
    public const string Otp_Send = "otp:send";
    public const string Otp_Verify = "otp:verify";

    // Admin permissions
    public const string Admin_FullAccess = "admin:full_access";

    // Core investment case permissions
    public const string InvestmentCases_Read = "investment_cases:read";
    public const string InvestmentCases_Write = "investment_cases:write";
    public const string InvestmentCases_Review = "investment_cases:review";
    public const string InvestmentCases_FinanceReview = "investment_cases:finance_review";
    public const string InvestmentCases_LegalReview = "investment_cases:legal_review";
    public const string InvestmentCases_CeoApprove = "investment_cases:ceo_approve";
}

public static class RolePermissions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> RolePermissionMappings = new Dictionary<string, IReadOnlyCollection<string>>
    {
        ["Admin"] = new[]
        {
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
            Permissions.Admin_FullAccess
        },
        ["User"] = new[]
        {
            Permissions.Users_Read,
            Permissions.Companies_Read,
            Permissions.Companies_Write,
            Permissions.Sessions_Read,
            Permissions.Otp_Send,
            Permissions.Otp_Verify
        },
        ["InvestmentExpert"] = new[]
        {
            Permissions.Users_Read,
            Permissions.Companies_Read,
            Permissions.Companies_Write,
            Permissions.Sessions_Read,
            Permissions.Otp_Send,
            Permissions.Otp_Verify,
            Permissions.InvestmentCases_Read,
            Permissions.InvestmentCases_Review
        },
        ["InvestmentManager"] = new[]
        {
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
        },
        ["CEO"] = new[]
        {
            Permissions.Users_Read,
            Permissions.Companies_Read,
            Permissions.Sessions_Read,
            Permissions.InvestmentCases_Read,
            Permissions.InvestmentCases_CeoApprove
        },
        ["LegalUnit"] = new[]
        {
            Permissions.Users_Read,
            Permissions.Companies_Read,
            Permissions.Sessions_Read,
            Permissions.InvestmentCases_Read,
            Permissions.InvestmentCases_LegalReview
        },
        ["FinancialUnit"] = new[]
        {
            Permissions.Users_Read,
            Permissions.Companies_Read,
            Permissions.Companies_Write,
            Permissions.Sessions_Read,
            Permissions.InvestmentCases_Read,
            Permissions.InvestmentCases_FinanceReview
        },
        ["TechnicalExpert"] = new[]
        {
            Permissions.Users_Read,
            Permissions.Sessions_Read,
            Permissions.Otp_Send,
            Permissions.Otp_Verify,
            Permissions.InvestmentCases_Read,
            Permissions.InvestmentCases_Review
        }
    };
}
