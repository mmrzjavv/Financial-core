using BuildingBlocks.Domain.Entities;

namespace Core.Domain.Entities;

public sealed class CaseEvaluationItem : Entity<Guid>
{
    private CaseEvaluationItem()
    {
        Title = default!;
    }

    public CaseEvaluationItem(Guid evaluationId, string title, bool isApproved, string? comment)
    {
        Id = Guid.NewGuid();
        EvaluationId = evaluationId;
        Title = title;
        IsApproved = isApproved;
        Comment = comment;
    }

    public Guid EvaluationId { get; private set; }
    public CaseEvaluation Evaluation { get; private set; } = default!;

    public string Title { get; private set; }
    public bool IsApproved { get; private set; }
    public string? Comment { get; private set; }
}

