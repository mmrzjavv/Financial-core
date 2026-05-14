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
                CasePermissions.ManageValuations,
                CasePermissions.DownloadDocuments
            },
            [SystemRoles.LegalExpert] = new[]
            {
                CasePermissions.ReadAll,
                CasePermissions.ViewInternalComments,
                CasePermissions.ManageContracts,
                CasePermissions.UploadDocuments,
                CasePermissions.DownloadDocuments
            },
            [SystemRoles.FinancialExpert] = new[]
            {
                CasePermissions.ReadAll,
                CasePermissions.ViewInternalComments,
                CasePermissions.ManagePayments,
                CasePermissions.ManageFinancialWorksheet,
                CasePermissions.DownloadDocuments
            }
        };

    public string? UserId => userContext.UserId;

    public bool IsInternalUser =>
        userContext.Roles.Contains(SystemRoles.Admin) ||
        userContext.Roles.Contains(SystemRoles.InvestmentExpert) ||
        userContext.Roles.Contains(SystemRoles.InvestmentManager) ||
        userContext.Roles.Contains(SystemRoles.FinancialExpert) ||
        userContext.Roles.Contains(SystemRoles.LegalExpert);

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
            if (!RolePermissions.TryGetValue(role, out var permissions))
                continue;

            if (permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}