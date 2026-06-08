namespace Core.Application.Abstractions;

public sealed record LoanInstallmentListProjection(
    Guid Id,
    int RowNumber,
    DateOnly InstallmentDate,
    decimal PrincipalAmount,
    decimal ProfitAmount,
    decimal TotalAmount,
    decimal FundShareOfPrincipal,
    decimal FundShareOfProfit,
    decimal FundShareOfTotal,
    bool IsGracePeriod,
    bool IsPaid,
    DateTimeOffset? PaidAt);
