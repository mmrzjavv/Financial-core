using BuildingBlocks.Application.Results;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Abstractions;

public interface ILoanCaseStateManager
{
    bool IsTerminalState(LoanCaseStatus status);

    bool CanTransition(
        LoanCaseStatus currentStatus,
        LoanWorkflowAction action,
        string userRole,
        out LoanCaseStatus nextStatus,
        out string errorMessage);

    Task<Result> TransitionAsync(
        LoanCase caseEntity,
        LoanWorkflowAction action,
        string actorId,
        string actorRole,
        string? comment = null,
        Guid? correlationId = null);

    IEnumerable<LoanWorkflowAction> GetAllowedActions(LoanCaseStatus currentStatus, string userRole);
}
