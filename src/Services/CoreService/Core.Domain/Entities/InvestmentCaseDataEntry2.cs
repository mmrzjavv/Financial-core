using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;

namespace Core.Domain.Entities;

public sealed class InvestmentCaseDataEntry2 : Entity<Guid>, IAuditableEntity
{
    private InvestmentCaseDataEntry2()
    {
        InvestmentAttractionBasis = default!;
    }

    public InvestmentCaseDataEntry2(Guid caseId, string investmentAttractionBasis)
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
