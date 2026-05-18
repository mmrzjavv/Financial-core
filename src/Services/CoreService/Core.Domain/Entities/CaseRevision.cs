using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities;

public sealed class CaseRevision : Entity<Guid>, IAuditableEntity
{
    private CaseRevision()
    {
        SubmittedByUserId = default!;
    }

    public CaseRevision(
        Guid caseId,
        CasePhase phase,
        int revisionNumber,
        string submittedByUserId,
        DateTimeOffset submittedAt)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        Phase = phase;
        RevisionNumber = revisionNumber;
        SubmittedByUserId = submittedByUserId;
        SubmittedAt = submittedAt;
        ReviewResult = ReviewResult.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid CaseId { get; private set; }
    public InvestmentCase Case { get; private set; } = default!;

    public CasePhase Phase { get; private set; }
    public int RevisionNumber { get; private set; }
    public string SubmittedByUserId { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }

    public string? ReviewedByUserId { get; private set; }
    public ReviewResult ReviewResult { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void MarkReviewed(string reviewedByUserId, ReviewResult result, DateTimeOffset reviewedAt)
    {
        ReviewedByUserId = reviewedByUserId;
        ReviewResult = result;
        ReviewedAt = reviewedAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

