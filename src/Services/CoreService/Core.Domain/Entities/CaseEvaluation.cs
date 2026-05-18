using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities;

public sealed class CaseEvaluation : Entity<Guid>, IAuditableEntity
{
    private CaseEvaluation()
    {
        ReviewerUserId = default!;
        ReviewerRole = default!;
    }

    public CaseEvaluation(Guid caseId, CasePhase phase, string reviewerUserId, string reviewerRole, string? notes)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        Phase = phase;
        ReviewerUserId = reviewerUserId;
        ReviewerRole = reviewerRole;
        Notes = notes;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid CaseId { get; private set; }
    public InvestmentCase Case { get; private set; } = default!;

    public CasePhase Phase { get; private set; }
    public string ReviewerUserId { get; private set; }
    public string ReviewerRole { get; private set; }
    public string? Notes { get; private set; }

    public List<CaseEvaluationItem> Items { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void SetItems(IEnumerable<CaseEvaluationItem> items)
    {
        Items = items.ToList();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

