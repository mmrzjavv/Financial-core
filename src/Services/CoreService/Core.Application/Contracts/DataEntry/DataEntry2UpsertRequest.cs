namespace Services.CoreService.Core.Application.Contracts.DataEntry;

public sealed record DataEntry2UpsertRequest(
    string MarketAnalysis,
    string RevenueModel,
    string CompetitiveAdvantage,
    string FinancialProjection,
    string? Risks,
    string? GoToMarketStrategy);

