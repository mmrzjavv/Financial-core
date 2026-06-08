using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities.Loan;

public sealed class LoanCaseWorkflowHistory : Entity<Guid>, IAuditableEntity
{
    private LoanCaseWorkflowHistory()
    {
        ChangedByUserId = default!;
        Action = default!;
        ActorRole = default!;
    }

    public LoanCaseWorkflowHistory(
        Guid caseId,
        LoanCasePhase fromPhase,
        LoanCasePhase toPhase,
        LoanCaseStatus fromStatus,
        LoanCaseStatus toStatus,
        string changedByUserId,
        string action,
        string actorRole,
        Guid correlationId,
        string? comment)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        FromPhase = fromPhase;
        ToPhase = toPhase;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedByUserId = changedByUserId;
        Action = action;
        ActorRole = actorRole;
        CorrelationId = correlationId;
        Comment = comment;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid CaseId { get; private set; }
    public LoanCase Case { get; private set; } = default!;

    public LoanCasePhase FromPhase { get; private set; }
    public LoanCasePhase ToPhase { get; private set; }
    public LoanCaseStatus FromStatus { get; private set; }
    public LoanCaseStatus ToStatus { get; private set; }
    public string ChangedByUserId { get; private set; }
    public string Action { get; private set; }
    public string ActorRole { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string? Comment { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
}
