namespace Core.Application.Requests;

public sealed record UpdateDataEntry2Request(
    string MarketAnalysis,
    string RevenueModel,
    string CompetitiveAdvantage,
    string? FinancialProjection);

