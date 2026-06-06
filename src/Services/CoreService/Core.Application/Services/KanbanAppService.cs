using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Domain.Abstractions;
using Core.Application.Abstractions;
using Core.Application.Authorization;
using Core.Application.Common;
using Core.Application.DTOs;
using Core.Application.Kanban;
using Core.Domain.Enums;
using Core.Domain.Identity;

namespace Core.Application.Services;

public sealed class KanbanAppService(
    ICoreUnitOfWork unitOfWork,
    ICaseAuthorizationService caseAuthorizationService,
    IGuaranteeAuthorizationService guaranteeAuthorizationService,
    ILoanAuthorizationService loanAuthorizationService,
    IUserContext userContext,
    ICaseStateManager investmentStateManager,
    IGuaranteeCaseStateManager guaranteeStateManager,
    ILoanCaseStateManager loanStateManager) : IKanbanAppService
{
    private const string InvestmentApiBase = "/api/v1/investmentcases";
    private const string GuaranteeApiBase = "/api/v1/guaranteecases";
    private const string RenewalApiBase = "/api/v1/guarantee-renewals";
    private const string LoanApiBase = "/api/v1/loancases";

    public Task<Result<IReadOnlyList<KanbanCaseCardDto>>> GetActionRequiredAsync(CancellationToken cancellationToken)
        => GetActionRequiredAsync(module: null, cancellationToken);

    public Task<Result<IReadOnlyList<KanbanCaseCardDto>>> GetActionRequiredInvestmentOnlyAsync(CancellationToken cancellationToken)
        => GetActionRequiredAsync(CaseModuleType.Investment, cancellationToken);

    public Task<Result<IReadOnlyList<KanbanCaseSummaryDto>>> GetWatchingAsync(CancellationToken cancellationToken)
        => GetWatchingAsync(module: null, cancellationToken);

    public Task<Result<IReadOnlyList<KanbanCaseSummaryDto>>> GetWatchingInvestmentOnlyAsync(CancellationToken cancellationToken)
        => GetWatchingAsync(CaseModuleType.Investment, cancellationToken);

    private async Task<Result<IReadOnlyList<KanbanCaseCardDto>>> GetActionRequiredAsync(
        CaseModuleType? module,
        CancellationToken cancellationToken)
    {
        var access = EnsureKanbanAccess();
        if (access.IsFailure)
            return Result<IReadOnlyList<KanbanCaseCardDto>>.Fail(access.Error!);

        var (userId, role) = access.Value!;
        var cards = new List<KanbanCaseCardDto>();

        if (module is null or CaseModuleType.Investment)
            cards.AddRange(await LoadInvestmentActionCardsAsync(userId, role, cancellationToken));

        if (module is null or CaseModuleType.Guarantee)
            cards.AddRange(await LoadGuaranteeActionCardsAsync(userId, role, cancellationToken));

        if (module is null or CaseModuleType.GuaranteeRenewal)
            cards.AddRange(await LoadRenewalActionCardsAsync(userId, role, cancellationToken));

        if (module is null or CaseModuleType.Loan)
            cards.AddRange(await LoadLoanActionCardsAsync(userId, cancellationToken));

        var sorted = cards.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).ToArray();
        return Result<IReadOnlyList<KanbanCaseCardDto>>.Ok(sorted);
    }

    private async Task<Result<IReadOnlyList<KanbanCaseSummaryDto>>> GetWatchingAsync(
        CaseModuleType? module,
        CancellationToken cancellationToken)
    {
        var access = EnsureKanbanAccess();
        if (access.IsFailure)
            return Result<IReadOnlyList<KanbanCaseSummaryDto>>.Fail(access.Error!);

        var (userId, role) = access.Value!;
        var items = new List<KanbanCaseSummaryDto>();

        if (module is null or CaseModuleType.Investment)
            items.AddRange(await LoadInvestmentWatchAsync(userId, role, cancellationToken));

        if (module is null or CaseModuleType.Guarantee)
            items.AddRange(await LoadGuaranteeWatchAsync(userId, role, cancellationToken));

        if (module is null or CaseModuleType.GuaranteeRenewal)
            items.AddRange(await LoadRenewalWatchAsync(userId, role, cancellationToken));

        if (module is null or CaseModuleType.Loan)
            items.AddRange(await LoadLoanWatchAsync(userId, cancellationToken));

        return Result<IReadOnlyList<KanbanCaseSummaryDto>>.Ok(items.OrderByDescending(x => x.CreatedAt).ToArray());
    }

    private async Task<IEnumerable<KanbanCaseCardDto>> LoadInvestmentActionCardsAsync(
        string userId,
        string role,
        CancellationToken ct)
    {
        var projections = await unitOfWork.InvestmentCases.ListActiveKanbanProjectionsAsync(
            userId, caseAuthorizationService.IsInternalUser, ct);

        return projections
            .Where(x => CaseKanbanRules.IsActionRequired(x.CurrentStatus, role))
            .Select(x =>
            {
                var allowed = investmentStateManager
                    .GetAllowedActions(x.CurrentStatus, role)
                    .Select(a => a.ToString())
                    .ToArray();

                return new KanbanCaseCardDto(
                    x.Id,
                    x.CaseNumber,
                    CaseModuleType.Investment,
                    InvestmentApiBase,
                    nameof(CaseStatus),
                    (int)x.CurrentStatus,
                    CaseKanbanRules.GetPhaseTitle(x.CurrentPhase),
                    CaseKanbanRules.GetStatusTitle(x.CurrentStatus),
                    x.ApplicantType,
                    x.StartupTitle,
                    x.CompanyName,
                    x.CreatedAt,
                    x.UpdatedAt,
                    CaseKanbanRules.GetPendingActionLabel(x.CurrentStatus, role),
                    allowed);
            });
    }

    private async Task<IEnumerable<KanbanCaseCardDto>> LoadGuaranteeActionCardsAsync(
        string userId,
        string role,
        CancellationToken ct)
    {
        var gRole = GuaranteeKanbanRules.ResolveWorkflowRole(userContext.Roles);
        var projections = await unitOfWork.GuaranteeCases.ListActiveKanbanProjectionsAsync(
            userId, guaranteeAuthorizationService.IsInternalUser, ct);

        return projections
            .Where(x => GuaranteeKanbanRules.IsActionRequired(x.CurrentStatus, gRole))
            .Select(x =>
            {
                var allowed = guaranteeStateManager
                    .GetAllowedActions(x.CurrentStatus, gRole)
                    .Select(a => a.ToString())
                    .ToArray();

                return new KanbanCaseCardDto(
                    x.Id,
                    x.CaseNumber,
                    CaseModuleType.Guarantee,
                    GuaranteeApiBase,
                    nameof(GuaranteeCaseStatus),
                    (int)x.CurrentStatus,
                    GuaranteeKanbanRules.GetPhaseTitle(x.CurrentPhase),
                    GuaranteeKanbanRules.GetStatusTitle(x.CurrentStatus),
                    x.ApplicantType,
                    x.RepresentativeName,
                    x.CompanyName,
                    x.CreatedAt,
                    x.UpdatedAt,
                    GuaranteeKanbanRules.GetPendingActionLabel(x.CurrentStatus, gRole),
                    allowed);
            });
    }

    private async Task<IEnumerable<KanbanCaseCardDto>> LoadRenewalActionCardsAsync(
        string userId,
        string role,
        CancellationToken ct)
    {
        var projections = await unitOfWork.GuaranteeRenewals.ListActiveKanbanProjectionsAsync(
            userId, guaranteeAuthorizationService.IsInternalUser, ct);

        return projections
            .Where(x => IsRenewalActionRequired(x.CurrentStatus, role))
            .Select(x => new KanbanCaseCardDto(
                x.Id,
                x.CaseNumber,
                CaseModuleType.GuaranteeRenewal,
                RenewalApiBase,
                nameof(GuaranteeRenewalStatus),
                (int)x.CurrentStatus,
                "تمدید",
                GetRenewalStatusTitle(x.CurrentStatus),
                null,
                x.ParentCaseNumber,
                null,
                x.CreatedAt,
                x.UpdatedAt,
                "اقدام شما لازم است",
                []));
    }

    private async Task<IEnumerable<KanbanCaseCardDto>> LoadLoanActionCardsAsync(
        string userId,
        CancellationToken ct)
    {
        var lRole = LoanKanbanRules.ResolveWorkflowRole(userContext.Roles);
        var projections = await unitOfWork.LoanCases.ListActiveKanbanProjectionsAsync(
            userId, loanAuthorizationService.IsInternalUser, ct);

        return projections
            .Where(x => LoanKanbanRules.IsActionRequired(x.CurrentStatus, lRole))
            .Select(x =>
            {
                var allowed = loanStateManager
                    .GetAllowedActions(x.CurrentStatus, lRole)
                    .Select(a => a.ToString())
                    .ToArray();

                return new KanbanCaseCardDto(
                    x.Id,
                    x.CaseNumber,
                    CaseModuleType.Loan,
                    LoanApiBase,
                    nameof(LoanCaseStatus),
                    (int)x.CurrentStatus,
                    LoanKanbanRules.GetPhaseTitle(x.CurrentPhase),
                    LoanKanbanRules.GetStatusTitle(x.CurrentStatus),
                    x.ApplicantType,
                    x.RequestedAmount?.ToString("N0"),
                    x.CompanyName,
                    x.CreatedAt,
                    x.UpdatedAt,
                    LoanKanbanRules.GetPendingActionLabel(x.CurrentStatus, lRole),
                    allowed);
            });
    }

    private async Task<IEnumerable<KanbanCaseSummaryDto>> LoadInvestmentWatchAsync(
        string userId,
        string role,
        CancellationToken ct)
    {
        var projections = await unitOfWork.InvestmentCases.ListActiveKanbanProjectionsAsync(
            userId, caseAuthorizationService.IsInternalUser, ct);

        return projections
            .Where(x => CaseKanbanRules.IsWatching(x.CurrentStatus, role))
            .Select(x => new KanbanCaseSummaryDto(
                x.Id,
                x.CaseNumber,
                CaseModuleType.Investment,
                InvestmentApiBase,
                nameof(CaseStatus),
                (int)x.CurrentStatus,
                CaseKanbanRules.GetPhaseTitle(x.CurrentPhase),
                CaseKanbanRules.GetStatusTitle(x.CurrentStatus),
                x.StartupTitle,
                x.CreatedAt,
                CaseKanbanRules.GetPendingActionLabel(x.CurrentStatus, role)));
    }

    private async Task<IEnumerable<KanbanCaseSummaryDto>> LoadGuaranteeWatchAsync(
        string userId,
        string role,
        CancellationToken ct)
    {
        var gRole = GuaranteeKanbanRules.ResolveWorkflowRole(userContext.Roles);
        var projections = await unitOfWork.GuaranteeCases.ListActiveKanbanProjectionsAsync(
            userId, guaranteeAuthorizationService.IsInternalUser, ct);

        return projections
            .Where(x => GuaranteeKanbanRules.IsWatching(x.CurrentStatus, gRole))
            .Select(x => new KanbanCaseSummaryDto(
                x.Id,
                x.CaseNumber,
                CaseModuleType.Guarantee,
                GuaranteeApiBase,
                nameof(GuaranteeCaseStatus),
                (int)x.CurrentStatus,
                GuaranteeKanbanRules.GetPhaseTitle(x.CurrentPhase),
                GuaranteeKanbanRules.GetStatusTitle(x.CurrentStatus),
                x.RepresentativeName,
                x.CreatedAt,
                GuaranteeKanbanRules.GetPendingActionLabel(x.CurrentStatus, gRole)));
    }

    private async Task<IEnumerable<KanbanCaseSummaryDto>> LoadRenewalWatchAsync(
        string userId,
        string role,
        CancellationToken ct)
    {
        var projections = await unitOfWork.GuaranteeRenewals.ListActiveKanbanProjectionsAsync(
            userId, guaranteeAuthorizationService.IsInternalUser, ct);

        return projections
            .Where(x => IsRenewalWatching(x.CurrentStatus, role))
            .Select(x => new KanbanCaseSummaryDto(
                x.Id,
                x.CaseNumber,
                CaseModuleType.GuaranteeRenewal,
                RenewalApiBase,
                nameof(GuaranteeRenewalStatus),
                (int)x.CurrentStatus,
                "تمدید",
                GetRenewalStatusTitle(x.CurrentStatus),
                x.ParentCaseNumber,
                x.CreatedAt,
                "در جریان بررسی"));
    }

    private async Task<IEnumerable<KanbanCaseSummaryDto>> LoadLoanWatchAsync(
        string userId,
        CancellationToken ct)
    {
        var lRole = LoanKanbanRules.ResolveWorkflowRole(userContext.Roles);
        var projections = await unitOfWork.LoanCases.ListActiveKanbanProjectionsAsync(
            userId, loanAuthorizationService.IsInternalUser, ct);

        return projections
            .Where(x => LoanKanbanRules.IsWatching(x.CurrentStatus, lRole))
            .Select(x => new KanbanCaseSummaryDto(
                x.Id,
                x.CaseNumber,
                CaseModuleType.Loan,
                LoanApiBase,
                nameof(LoanCaseStatus),
                (int)x.CurrentStatus,
                LoanKanbanRules.GetPhaseTitle(x.CurrentPhase),
                LoanKanbanRules.GetStatusTitle(x.CurrentStatus),
                x.CompanyName ?? x.RequestedAmount?.ToString("N0"),
                x.CreatedAt,
                LoanKanbanRules.GetPendingActionLabel(x.CurrentStatus, lRole)));
    }

    private static bool IsRenewalActionRequired(GuaranteeRenewalStatus status, string role) => (status, role) switch
    {
        (GuaranteeRenewalStatus.Draft, UserRoleClaims.Applicant) => true,
        (GuaranteeRenewalStatus.CeoReview, UserRoleClaims.Ceo) => true,
        (GuaranteeRenewalStatus.CreditDateUpdate, UserRoleClaims.CreditExpert) => true,
        (GuaranteeRenewalStatus.CreditDateUpdate, UserRoleClaims.CreditManager) => true,
        _ => string.Equals(role, UserRoleClaims.Admin, StringComparison.OrdinalIgnoreCase) && status is not (GuaranteeRenewalStatus.Completed or GuaranteeRenewalStatus.Rejected or GuaranteeRenewalStatus.Cancelled)
    };

    private static bool IsRenewalWatching(GuaranteeRenewalStatus status, string role)
        => !IsRenewalActionRequired(status, role) && status is GuaranteeRenewalStatus.CeoReview or GuaranteeRenewalStatus.CreditDateUpdate;

    private static string GetRenewalStatusTitle(GuaranteeRenewalStatus status) => status switch
    {
        GuaranteeRenewalStatus.Draft => "پیش‌نویس تمدید",
        GuaranteeRenewalStatus.CeoReview => "بررسی مدیرعامل",
        GuaranteeRenewalStatus.CreditDateUpdate => "به‌روزرسانی تاریخ",
        GuaranteeRenewalStatus.Completed => "تکمیل",
        GuaranteeRenewalStatus.Rejected => "رد شده",
        GuaranteeRenewalStatus.Cancelled => "لغو",
        _ => status.ToString()
    };

    private Result<(string UserId, string Role)> EnsureKanbanAccess()
    {
        var auth = caseAuthorizationService.EnsureAuthenticated();
        if (auth.IsFailure) return Result<(string, string)>.Fail(auth.Error!);

        var canRead = caseAuthorizationService.HasPermission(CasePermissions.ReadOwn)
                        || caseAuthorizationService.HasPermission(CasePermissions.ReadAll)
                        || guaranteeAuthorizationService.HasPermission(GuaranteePermissions.ReadOwn)
                        || guaranteeAuthorizationService.HasPermission(GuaranteePermissions.ReadAll)
                        || loanAuthorizationService.HasPermission(LoanPermissions.ReadOwn)
                        || loanAuthorizationService.HasPermission(LoanPermissions.ReadAll);

        if (!canRead)
            return Result<(string, string)>.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        var role = GuaranteeKanbanRules.ResolveWorkflowRole(userContext.Roles);
        if (string.IsNullOrWhiteSpace(role))
            role = CaseKanbanRules.ResolveWorkflowRole(userContext.Roles);

        if (string.IsNullOrWhiteSpace(role))
            role = LoanKanbanRules.ResolveWorkflowRole(userContext.Roles);

        if (string.IsNullOrWhiteSpace(role))
            return Result<(string, string)>.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        return Result<(string, string)>.Ok((caseAuthorizationService.UserId!, role));
    }
}
