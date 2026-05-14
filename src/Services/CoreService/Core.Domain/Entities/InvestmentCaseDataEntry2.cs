using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;

namespace Services.CoreService.Core.Domain.Entities;

public sealed class InvestmentCaseDataEntry2 : Entity<Guid>, IAuditableEntity
{
    private InvestmentCaseDataEntry2()
    {
        MarketAnalysis = default!;
        RevenueModel = default!;
        CompetitiveAdvantage = default!;
        FinancialProjection = default!;
    }

    public InvestmentCaseDataEntry2(
        Guid caseId,
        string marketAnalysis,
        string revenueModel,
        string competitiveAdvantage,
        string financialProjection)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        MarketAnalysis = marketAnalysis;
        RevenueModel = revenueModel;
        CompetitiveAdvantage = competitiveAdvantage;
        FinancialProjection = financialProjection;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid CaseId { get; private set; }
    public InvestmentCase Case { get; private set; } = default!;

    public string MarketAnalysis { get; private set; }
    public string RevenueModel { get; private set; }
    public string CompetitiveAdvantage { get; private set; }
    public string FinancialProjection { get; private set; }

    public string? Risks { get; private set; }
    public string? GoToMarketStrategy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Update(
        string marketAnalysis,
        string revenueModel,
        string competitiveAdvantage,
        string financialProjection,
        string? risks,
        string? goToMarketStrategy)
    {
        MarketAnalysis = marketAnalysis;
        RevenueModel = revenueModel;
        CompetitiveAdvantage = competitiveAdvantage;
        FinancialProjection = financialProjection;
        Risks = risks;
        GoToMarketStrategy = goToMarketStrategy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

