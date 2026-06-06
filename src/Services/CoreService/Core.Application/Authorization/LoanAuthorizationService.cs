using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Domain.Abstractions;
using Core.Application.Common;
using Core.Domain.Identity;

namespace Core.Application.Authorization;

public sealed class LoanAuthorizationService(IUserContext userContext) : ILoanAuthorizationService
{
    private static readonly string[] CreditUnitPermissions =
    [
        LoanPermissions.ReadAll,
        LoanPermissions.ViewInternalComments,
        LoanPermissions.CreateInternalComment,
        LoanPermissions.ManageApprovalDetail,
        LoanPermissions.DownloadDocuments
    ];

    private static readonly string[] LegalUnitPermissions =
    [
        LoanPermissions.ReadAll,
        LoanPermissions.ViewInternalComments,
        LoanPermissions.CreateInternalComment,
        LoanPermissions.ManageContracts,
        LoanPermissions.ManageInstallments,
        LoanPermissions.UploadDocuments,
        LoanPermissions.DownloadDocuments
    ];

    private static readonly string[] FinancialUnitPermissions =
    [
        LoanPermissions.ReadAll,
        LoanPermissions.ViewInternalComments,
        LoanPermissions.CreateInternalComment,
        LoanPermissions.ManagePayments,
        LoanPermissions.UploadDocuments,
        LoanPermissions.DownloadDocuments
    ];

    private static readonly string[] AllLoanPermissions =
    [
        LoanPermissions.Create,
        LoanPermissions.ReadAll,
        LoanPermissions.ReadOwn,
        LoanPermissions.ViewInternalComments,
        LoanPermissions.CreateInternalComment,
        LoanPermissions.ManageApprovalDetail,
        LoanPermissions.ManageContracts,
        LoanPermissions.ManageInstallments,
        LoanPermissions.ManagePayments,
        LoanPermissions.CeoApprove,
        LoanPermissions.UploadDocuments,
        LoanPermissions.DownloadDocuments
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> RolePermissions =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [UserRoleClaims.Applicant] =
            [
                LoanPermissions.Create,
                LoanPermissions.ReadOwn,
                LoanPermissions.UploadDocuments,
                LoanPermissions.DownloadDocuments
            ],
            [UserRoleClaims.CreditExpert] = CreditUnitPermissions,
            [UserRoleClaims.CreditManager] = CreditUnitPermissions,
            [UserRoleClaims.LegalExpert] = LegalUnitPermissions,
            [UserRoleClaims.LegalManager] = LegalUnitPermissions,
            [UserRoleClaims.FinancialExpert] = FinancialUnitPermissions,
            [UserRoleClaims.FinancialManager] = FinancialUnitPermissions,
            [UserRoleClaims.Ceo] =
            [
                LoanPermissions.ReadAll,
                LoanPermissions.CeoApprove
            ],
            [UserRoleClaims.Admin] = AllLoanPermissions
        };

    public string? UserId => userContext.UserId;

    public bool IsInternalUser =>
        userContext.Roles.Any(r =>
            !string.Equals(UserRoleClaims.Applicant, UserRoleClaims.Normalize(r), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals("User", r, StringComparison.OrdinalIgnoreCase));

    public Result<string> EnsureAuthenticated()
    {
        if (string.IsNullOrWhiteSpace(userContext.UserId))
            return Result<string>.Fail(Error.Unauthorized(ApiMessages.AuthenticationRequired));

        return Result<string>.Ok(userContext.UserId);
    }

    public bool HasPermission(string permission)
    {
        if (userContext.Roles.Contains(UserRoleClaims.Admin))
            return true;

        foreach (var role in userContext.Roles)
        {
            var normalized = UserRoleClaims.Normalize(role);
            if (RolePermissions.TryGetValue(normalized, out var permissions) && permissions.Contains(permission))
                return true;
        }

        return false;
    }
}
