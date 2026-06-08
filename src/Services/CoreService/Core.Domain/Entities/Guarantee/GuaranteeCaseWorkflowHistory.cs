using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities.Guarantee;

public sealed class GuaranteeCaseWorkflowHistory : Entity<Guid>, IAuditableEntity
{
    private GuaranteeCaseWorkflowHistory()
    {
        ChangedByUserId = default!;
        Action = default!;
        ActorRole = default!;
    }

    public GuaranteeCaseWorkflowHistory(
        Guid caseId,
        GuaranteeCasePhase fromPhase,
        GuaranteeCasePhase toPhase,
        GuaranteeCaseStatus fromStatus,
        GuaranteeCaseStatus toStatus,
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
    public GuaranteeCase Case { get; private set; } = default!;

    public GuaranteeCasePhase FromPhase { get; private set; }
    public GuaranteeCasePhase ToPhase { get; private set; }
    public GuaranteeCaseStatus FromStatus { get; private set; }
    public GuaranteeCaseStatus ToStatus { get; private set; }
    public string ChangedByUserId { get; private set; }
    public string Action { get; private set; }
    public string ActorRole { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string? Comment { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
}
