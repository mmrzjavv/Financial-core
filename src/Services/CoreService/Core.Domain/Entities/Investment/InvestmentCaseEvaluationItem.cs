using BuildingBlocks.Domain.Entities;

namespace Core.Domain.Entities.Investment;

public sealed class InvestmentCaseEvaluationItem : Entity<Guid>
{
    private InvestmentCaseEvaluationItem()
    {
        Title = default!;
    }

    public InvestmentCaseEvaluationItem(Guid evaluationId, string title, bool isApproved, string? comment)
    {
        Id = Guid.NewGuid();
        EvaluationId = evaluationId;
        Title = title;
        IsApproved = isApproved;
        Comment = comment;
    }

    public Guid EvaluationId { get; private set; }
    public InvestmentCaseEvaluation Evaluation { get; private set; } = default!;

    public string Title { get; private set; }
    public bool IsApproved { get; private set; }
    public string? Comment { get; private set; }
}

