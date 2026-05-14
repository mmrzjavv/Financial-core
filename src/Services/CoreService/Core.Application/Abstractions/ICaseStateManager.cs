using BuildingBlocks.Application.Results;
using Services.CoreService.Core.Domain.Entities;
using Services.CoreService.Core.Domain.Enums;


namespace Core.Application.Abstractions;

public interface ICaseStateManager
{
    bool CanTransition(
        CaseStatus currentStatus,
        WorkflowAction action,
        string userRole,
        out CaseStatus nextStatus,
        out string errorMessage);

    Task<Result> TransitionAsync(
        InvestmentCase caseEntity,
        WorkflowAction action,
        string actorId,
        string actorRole,
        string? comment = null,
        Guid? correlationId = null);

    bool IsTerminalState(CaseStatus status);

    IEnumerable<WorkflowAction> GetAllowedActions(CaseStatus currentStatus, string userRole);
}