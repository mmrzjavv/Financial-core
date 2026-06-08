using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;

namespace Core.Domain.Entities.Investment;

public sealed class InvestmentCaseAttractionBasis : Entity<Guid>, IAuditableEntity
{
    private InvestmentCaseAttractionBasis()
    {
        InvestmentAttractionBasis = default!;
    }

    public InvestmentCaseAttractionBasis(Guid caseId, string investmentAttractionBasis)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        InvestmentAttractionBasis = investmentAttractionBasis;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid CaseId { get; private set; }
    public InvestmentCase Case { get; private set; } = default!;

    /// <summary>مبنای درخواست جذب سرمایه‌گذار</summary>
    public string InvestmentAttractionBasis { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Update(string investmentAttractionBasis)
    {
        InvestmentAttractionBasis = investmentAttractionBasis;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
