using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities.Investment;

public sealed class InvestmentCaseEvaluation : Entity<Guid>, IAuditableEntity
{
    private InvestmentCaseEvaluation()
    {
        ReviewerUserId = default!;
        ReviewerRole = default!;
    }

    public InvestmentCaseEvaluation(Guid caseId, CasePhase phase, string reviewerUserId, string reviewerRole, string? notes)
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

    public List<InvestmentCaseEvaluationItem> Items { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void SetItems(IEnumerable<InvestmentCaseEvaluationItem> items)
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

