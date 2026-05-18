using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Domain.Abstractions;
using Core.Application.Abstractions;
using Core.Application.Authorization;
using Core.Application.Common;
using Core.Application.DTOs;
using Core.Application.Kanban;
using Core.Application.Logging;
using Microsoft.Extensions.Logging;

namespace Core.Application.Services;

public sealed class KanbanAppService(
    ICoreUnitOfWork unitOfWork,
    ICaseAuthorizationService authorizationService,
    IUserContext userContext,
    ICaseStateManager stateManager,
    ILogger<KanbanAppService> logger) : IKanbanAppService
{
    public async Task<Result<IReadOnlyList<KanbanCaseCardDto>>> GetActionRequiredAsync(CancellationToken cancellationToken)
    {
        var access = EnsureKanbanAccess();
        if (access.IsFailure)
        {
            ApplicationLog.Blocked(logger, "KanbanActionRequired", access.Error?.Message ?? "access denied");
            return Result<IReadOnlyList<KanbanCaseCardDto>>.Fail(access.Error!);
        }

        var (userId, role) = access.Value!;
        ApplicationLog.Started(logger, "KanbanActionRequired", userId);

        var projections = await unitOfWork.InvestmentCases.ListActiveKanbanProjectionsAsync(
            userId,
            authorizationService.IsInternalUser,
            cancellationToken);

        var cards = projections
            .Where(x => CaseKanbanRules.IsActionRequired(x.CurrentStatus, role))
            .Select(x => ToCard(x, role))
            .ToArray();

        ApplicationLog.Completed(logger,
            "User {UserId} (role {Role}) loaded kanban action-required board — {Count} case(s) need attention",
            userId, role, cards.Length);

        return Result<IReadOnlyList<KanbanCaseCardDto>>.Ok(cards);
    }

    public async Task<Result<IReadOnlyList<KanbanCaseSummaryDto>>> GetWatchingAsync(CancellationToken cancellationToken)
    {
        var access = EnsureKanbanAccess();
        if (access.IsFailure)
        {
            ApplicationLog.Blocked(logger, "KanbanWatching", access.Error?.Message ?? "access denied");
            return Result<IReadOnlyList<KanbanCaseSummaryDto>>.Fail(access.Error!);
        }

        var (userId, role) = access.Value!;
        ApplicationLog.Started(logger, "KanbanWatching", userId);

        var projections = await unitOfWork.InvestmentCases.ListActiveKanbanProjectionsAsync(
            userId,
            authorizationService.IsInternalUser,
            cancellationToken);

        var items = projections
            .Where(x => CaseKanbanRules.IsWatching(x.CurrentStatus, role))
            .Select(x => ToSummary(x, role))
            .ToArray();

        ApplicationLog.Completed(logger,
            "User {UserId} (role {Role}) loaded kanban watching list — {Count} case(s) in view",
            userId, role, items.Length);

        return Result<IReadOnlyList<KanbanCaseSummaryDto>>.Ok(items);
    }

    private Result<(string UserId, string Role)> EnsureKanbanAccess()
    {
        var auth = authorizationService.EnsureAuthenticated();
        if (auth.IsFailure) return Result<(string, string)>.Fail(auth.Error!);

        if (!authorizationService.IsInternalUser &&
            !authorizationService.HasPermission(CasePermissions.ReadOwn))
            return Result<(string, string)>.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        if (authorizationService.IsInternalUser &&
            !authorizationService.HasPermission(CasePermissions.ReadAll))
            return Result<(string, string)>.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        var role = CaseKanbanRules.ResolveWorkflowRole(userContext.Roles);
        if (string.IsNullOrWhiteSpace(role))
            return Result<(string, string)>.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        return Result<(string, string)>.Ok((authorizationService.UserId!, role));
    }

    private KanbanCaseCardDto ToCard(KanbanCaseProjection projection, string role)
    {
        var allowed = stateManager
            .GetAllowedActions(projection.CurrentStatus, role)
            .Select(a => a.ToString())
            .ToArray();

        return new KanbanCaseCardDto(
            projection.Id,
            projection.CaseNumber,
            projection.CurrentPhase,
            CaseKanbanRules.GetPhaseTitle(projection.CurrentPhase),
            projection.CurrentStatus,
            CaseKanbanRules.GetStatusTitle(projection.CurrentStatus),
            projection.ApplicantType,
            projection.StartupTitle,
            projection.CompanyName,
            projection.CreatedAt,
            projection.UpdatedAt,
            CaseKanbanRules.GetPendingActionLabel(projection.CurrentStatus, role),
            allowed);
    }

    private static KanbanCaseSummaryDto ToSummary(KanbanCaseProjection projection, string role)
        => new(
            projection.Id,
            projection.CaseNumber,
            projection.CurrentPhase,
            CaseKanbanRules.GetPhaseTitle(projection.CurrentPhase),
            projection.CurrentStatus,
            CaseKanbanRules.GetStatusTitle(projection.CurrentStatus),
            projection.StartupTitle,
            projection.CreatedAt,
            CaseKanbanRules.GetPendingActionLabel(projection.CurrentStatus, role));
}
