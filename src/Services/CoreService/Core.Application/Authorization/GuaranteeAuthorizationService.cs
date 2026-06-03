using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Domain.Abstractions;
using Core.Application.Common;
using Core.Domain.Identity;

namespace Core.Application.Authorization;

public sealed class GuaranteeAuthorizationService(IUserContext userContext) : IGuaranteeAuthorizationService
{
    private static readonly string[] CreditUnitPermissions =
    [
        GuaranteePermissions.ReadAll,
        GuaranteePermissions.ViewInternalComments,
        GuaranteePermissions.CreateInternalComment,
        GuaranteePermissions.ManageApprovalForm,
        GuaranteePermissions.DownloadDocuments
    ];

    private static readonly string[] LegalUnitPermissions =
    [
        GuaranteePermissions.ReadAll,
        GuaranteePermissions.ViewInternalComments,
        GuaranteePermissions.CreateInternalComment,
        GuaranteePermissions.ManageContracts,
        GuaranteePermissions.UploadDocuments,
        GuaranteePermissions.DownloadDocuments
    ];

    private static readonly string[] FinancialUnitPermissions =
    [
        GuaranteePermissions.ReadAll,
        GuaranteePermissions.ViewInternalComments,
        GuaranteePermissions.CreateInternalComment,
        GuaranteePermissions.ManageAttachments,
        GuaranteePermissions.ManageIssuance,
        GuaranteePermissions.UploadDocuments,
        GuaranteePermissions.DownloadDocuments
    ];

    /// <summary>Every guarantee-case permission (Admin uses this list explicitly).</summary>
    private static readonly string[] AllGuaranteePermissions =
    [
        GuaranteePermissions.Create,
        GuaranteePermissions.ReadAll,
        GuaranteePermissions.ReadOwn,
        GuaranteePermissions.ViewInternalComments,
        GuaranteePermissions.CreateInternalComment,
        GuaranteePermissions.ManageApprovalForm,
        GuaranteePermissions.ManageContracts,
        GuaranteePermissions.ManageAttachments,
        GuaranteePermissions.ManageIssuance,
        GuaranteePermissions.CeoApprove,
        GuaranteePermissions.SetApplicantCreditLimit,
        GuaranteePermissions.UploadDocuments,
        GuaranteePermissions.DownloadDocuments
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> RolePermissions =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [UserRoleClaims.Applicant] =
            [
                GuaranteePermissions.Create,
                GuaranteePermissions.ReadOwn,
                GuaranteePermissions.UploadDocuments,
                GuaranteePermissions.DownloadDocuments
            ],
            [UserRoleClaims.CreditExpert] = CreditUnitPermissions,
            [UserRoleClaims.CreditManager] = CreditUnitPermissions,
            [UserRoleClaims.LegalExpert] = LegalUnitPermissions,
            [UserRoleClaims.LegalManager] = LegalUnitPermissions,
            [UserRoleClaims.FinancialExpert] = FinancialUnitPermissions,
            [UserRoleClaims.FinancialManager] = FinancialUnitPermissions,
            [UserRoleClaims.Ceo] =
            [
                GuaranteePermissions.ReadAll,
                GuaranteePermissions.CeoApprove,
                GuaranteePermissions.SetApplicantCreditLimit
            ],
            [UserRoleClaims.Admin] = AllGuaranteePermissions,
            // Sample: full guarantee permissions. Remove entry to deny guarantee module access.
            [UserRoleClaims.TechnicalExpert] = AllGuaranteePermissions
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
