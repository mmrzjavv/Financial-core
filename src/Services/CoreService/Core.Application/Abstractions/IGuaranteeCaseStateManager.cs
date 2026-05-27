using BuildingBlocks.Application.Results;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Abstractions;

public interface IGuaranteeCaseStateManager
{
    bool IsTerminalState(GuaranteeCaseStatus status);
    Task<Result> TransitionAsync(
        GuaranteeCase caseEntity,
        GuaranteeWorkflowAction action,
        string actorId,
        string actorRole,
        string? comment = null,
        Guid? correlationId = null);
    IEnumerable<GuaranteeWorkflowAction> GetAllowedActions(GuaranteeCaseStatus currentStatus, string userRole);
}
