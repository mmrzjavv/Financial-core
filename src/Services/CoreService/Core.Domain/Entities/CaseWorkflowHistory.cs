using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Domain.Entities;

public sealed class CaseWorkflowHistory : Entity<Guid>, IAuditableEntity
{
    private CaseWorkflowHistory()
    {
        ChangedByUserId = default!;
        Action = default!;
        ActorRole = default!;
    }

    public CaseWorkflowHistory(
        Guid caseId,
        CasePhase fromPhase,
        CasePhase toPhase,
        CaseStatus fromStatus,
        CaseStatus toStatus,
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
    public InvestmentCase Case { get; private set; } = default!;

    public CasePhase FromPhase { get; private set; }
    public CasePhase ToPhase { get; private set; }
    public CaseStatus FromStatus { get; private set; }
    public CaseStatus ToStatus { get; private set; }
    public string ChangedByUserId { get; private set; }
    public string Action { get; private set; }
    public string ActorRole { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string? Comment { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
}

