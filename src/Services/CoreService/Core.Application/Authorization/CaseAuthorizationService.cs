using Core.Application.Common;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Domain.Abstractions;
using Core.Domain.Identity;


namespace Core.Application.Authorization;

public sealed class CaseAuthorizationService(IUserContext userContext) : ICaseAuthorizationService
{
    private static readonly string[] InvestmentExpertPermissions =
    [
        CasePermissions.ReadAll,
        CasePermissions.ViewInternalComments,
        CasePermissions.CreateInternalComment,
        CasePermissions.ViewEvaluations,
        CasePermissions.UpsertEvaluations,
        CasePermissions.ManageFinancialWorksheet,
        CasePermissions.UploadDocuments,
        CasePermissions.DownloadDocuments,
        CasePermissions.UploadCommentAttachments
    ];

    private static readonly string[] InvestmentManagerPermissions =
    [
        CasePermissions.ReadAll,
        CasePermissions.ViewInternalComments,
        CasePermissions.CreateInternalComment,
        CasePermissions.ManageValuations,
        CasePermissions.ViewEvaluations,
        CasePermissions.UpsertEvaluations,
        CasePermissions.ManageFinancialWorksheet,
        CasePermissions.DownloadDocuments,
        CasePermissions.UploadCommentAttachments
    ];

    private static readonly string[] LegalUnitPermissions =
    [
        CasePermissions.ReadAll,
        CasePermissions.ViewInternalComments,
        CasePermissions.CreateInternalComment,
        CasePermissions.ManageContracts,
        CasePermissions.UploadDocuments,
        CasePermissions.DownloadDocuments,
        CasePermissions.UploadCommentAttachments
    ];

    private static readonly string[] FinancialUnitPermissions =
    [
        CasePermissions.ReadAll,
        CasePermissions.ViewInternalComments,
        CasePermissions.CreateInternalComment,
        CasePermissions.ManagePayments,
        CasePermissions.ManageFinancialWorksheet,
        CasePermissions.DownloadDocuments,
        CasePermissions.UploadCommentAttachments
    ];

    private static readonly string[] TechnicalUnitPermissions =
    [
        CasePermissions.ReadAll,
        CasePermissions.ViewInternalComments,
        CasePermissions.CreateInternalComment,
        CasePermissions.ViewEvaluations,
        CasePermissions.DownloadDocuments,
        CasePermissions.UploadCommentAttachments
    ];

    /// <summary>Every investment-case permission (Admin bypasses the dictionary and does not need this).</summary>
    private static readonly string[] AllCasePermissions =
    [
        CasePermissions.Create,
        CasePermissions.ReadOwn,
        CasePermissions.ReadAll,
        CasePermissions.ViewInternalComments,
        CasePermissions.CreateInternalComment,
        CasePermissions.ViewEvaluations,
        CasePermissions.UpsertEvaluations,
        CasePermissions.ManageValuations,
        CasePermissions.ManageContracts,
        CasePermissions.ManageFinancialWorksheet,
        CasePermissions.ManagePayments,
        CasePermissions.CeoApprove,
        CasePermissions.UploadDocuments,
        CasePermissions.DownloadDocuments,
        CasePermissions.UploadCommentAttachments
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> RolePermissions =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [UserRoleClaims.Applicant] =
            [
                CasePermissions.Create,
                CasePermissions.ReadOwn,
                CasePermissions.UploadDocuments,
                CasePermissions.DownloadDocuments,
                CasePermissions.UploadCommentAttachments
            ],
            [UserRoleClaims.InvestmentExpert] = InvestmentExpertPermissions,
            [UserRoleClaims.InvestmentManager] = InvestmentManagerPermissions,
            [UserRoleClaims.LegalExpert] = LegalUnitPermissions,
            [UserRoleClaims.LegalManager] = LegalUnitPermissions,
            [UserRoleClaims.FinancialExpert] = FinancialUnitPermissions,
            [UserRoleClaims.FinancialManager] = FinancialUnitPermissions,
            // Sample: full case permissions. Revert to TechnicalUnitPermissions for production.
            [UserRoleClaims.TechnicalExpert] = AllCasePermissions,
            [UserRoleClaims.TechnicalManager] = TechnicalUnitPermissions,
            [UserRoleClaims.Ceo] =
            [
                CasePermissions.ReadAll,
                CasePermissions.ViewInternalComments,
                CasePermissions.CreateInternalComment,
                CasePermissions.CeoApprove,
                CasePermissions.DownloadDocuments,
                CasePermissions.UploadCommentAttachments
            ]
        };

    public string? UserId => userContext.UserId;

    public bool IsInternalUser =>
        userContext.Roles.Contains(UserRoleClaims.Admin) ||
        userContext.Roles.Contains(UserRoleClaims.InvestmentExpert) ||
        userContext.Roles.Contains(UserRoleClaims.InvestmentManager) ||
        userContext.Roles.Contains(UserRoleClaims.LegalExpert) ||
        userContext.Roles.Contains(UserRoleClaims.LegalManager) ||
        userContext.Roles.Contains(UserRoleClaims.FinancialExpert) ||
        userContext.Roles.Contains(UserRoleClaims.FinancialManager) ||
        userContext.Roles.Contains(UserRoleClaims.TechnicalExpert) ||
        userContext.Roles.Contains(UserRoleClaims.TechnicalManager) ||
        userContext.Roles.Contains(UserRoleClaims.Ceo) ||
        userContext.Roles.Contains(UserRoleClaims.LegalUnit, StringComparer.OrdinalIgnoreCase) ||
        userContext.Roles.Contains(UserRoleClaims.FinancialUnit, StringComparer.OrdinalIgnoreCase) ||
        userContext.Roles.Contains(UserRoleClaims.InvestmentUnit, StringComparer.OrdinalIgnoreCase) ||
        userContext.Roles.Contains("CEO", StringComparer.OrdinalIgnoreCase);

    public Result EnsureAuthenticated()
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return Result.Fail(Error.Unauthorized(ApiMessages.AuthenticationRequired));

        return Result.Ok();
    }

    public bool HasPermission(string permission)
    {
        if (userContext.Roles.Contains(UserRoleClaims.Admin))
            return true;

        if (string.Equals(permission, CasePermissions.ReadOwn, StringComparison.OrdinalIgnoreCase))
            return userContext.Roles.Contains(UserRoleClaims.Applicant);

        if (string.Equals(permission, CasePermissions.ReadAll, StringComparison.OrdinalIgnoreCase))
            return IsInternalUser;

        foreach (var role in userContext.Roles)
        {
            foreach (var permissionRole in ResolvePermissionRoles(role))
            {
                if (!RolePermissions.TryGetValue(permissionRole, out var permissions))
                    continue;

                if (permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> ResolvePermissionRoles(string role)
    {
        yield return role;

        var normalized = UserRoleClaims.Normalize(role);
        if (!string.Equals(normalized, role, StringComparison.OrdinalIgnoreCase))
            yield return normalized;
    }
}
