using Core.Application.Common;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Domain.Abstractions;
using Services.CoreService.Core.Domain.Constants;


namespace Core.Application.Authorization;

public sealed class CaseAuthorizationService(IUserContext userContext) : ICaseAuthorizationService
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> RolePermissions =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [SystemRoles.Applicant] = new[]
            {
                CasePermissions.Create,
                CasePermissions.ReadOwn,
                CasePermissions.UploadDocuments,
                CasePermissions.DownloadDocuments,
                CasePermissions.UploadCommentAttachments
            },
            [SystemRoles.InvestmentExpert] = new[]
            {
                CasePermissions.ReadAll,
                CasePermissions.ViewInternalComments,
                CasePermissions.CreateInternalComment,
                CasePermissions.ViewEvaluations,
                CasePermissions.UpsertEvaluations,
                CasePermissions.ManageFinancialWorksheet,
                CasePermissions.UploadDocuments,
                CasePermissions.DownloadDocuments,
                CasePermissions.UploadCommentAttachments
            },
            [SystemRoles.InvestmentManager] = new[]
            {
                CasePermissions.ReadAll,
                CasePermissions.ViewInternalComments,
                CasePermissions.CreateInternalComment,
                CasePermissions.ManageValuations,
                CasePermissions.DownloadDocuments,
                CasePermissions.UploadCommentAttachments
            },
            [SystemRoles.LegalExpert] = new[]
            {
                CasePermissions.ReadAll,
                CasePermissions.ViewInternalComments,
                CasePermissions.CreateInternalComment,
                CasePermissions.ManageContracts,
                CasePermissions.UploadDocuments,
                CasePermissions.DownloadDocuments,
                CasePermissions.UploadCommentAttachments
            },
            [SystemRoles.FinancialExpert] = new[]
            {
                CasePermissions.ReadAll,
                CasePermissions.ViewInternalComments,
                CasePermissions.CreateInternalComment,
                CasePermissions.ManagePayments,
                CasePermissions.ManageFinancialWorksheet,
                CasePermissions.DownloadDocuments,
                CasePermissions.UploadCommentAttachments
            }
        };

    public string? UserId => userContext.UserId;

    public bool IsInternalUser =>
        userContext.Roles.Contains(SystemRoles.Admin) ||
        userContext.Roles.Contains(SystemRoles.InvestmentExpert) ||
        userContext.Roles.Contains(SystemRoles.InvestmentManager) ||
        userContext.Roles.Contains(SystemRoles.FinancialExpert) ||
        userContext.Roles.Contains(SystemRoles.LegalExpert) ||
        userContext.Roles.Contains("LegalUnit", StringComparer.OrdinalIgnoreCase) ||
        userContext.Roles.Contains("FinancialUnit", StringComparer.OrdinalIgnoreCase) ||
        userContext.Roles.Contains("InvestmentUnit", StringComparer.OrdinalIgnoreCase);

    public Result EnsureAuthenticated()
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return Result.Fail(Error.Unauthorized(ApiMessages.AuthenticationRequired));

        return Result.Ok();
    }

    public bool HasPermission(string permission)
    {
        if (userContext.Roles.Contains(SystemRoles.Admin))
            return true;

        if (string.Equals(permission, CasePermissions.ReadOwn, StringComparison.OrdinalIgnoreCase))
            return userContext.Roles.Contains(SystemRoles.Applicant);

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

        if (role.Equals("LegalUnit", StringComparison.OrdinalIgnoreCase))
            yield return SystemRoles.LegalExpert;

        if (role.Equals("FinancialUnit", StringComparison.OrdinalIgnoreCase))
            yield return SystemRoles.FinancialExpert;

        if (role.Equals("InvestmentUnit", StringComparison.OrdinalIgnoreCase))
            yield return SystemRoles.InvestmentExpert;
    }
}