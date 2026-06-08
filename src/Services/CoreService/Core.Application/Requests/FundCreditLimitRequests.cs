using Core.Domain.Enums;

namespace Core.Application.Requests;

public sealed record CreateFundCreditLimitRequest(
    FundModuleType ModuleType,
    decimal CreditLimitWithCheck,
    DateOnly PeriodStart,
    DateOnly ExpiresAt);

public sealed record UpdateFundCreditLimitRequest(
    decimal CreditLimitWithCheck,
    DateOnly PeriodStart,
    DateOnly ExpiresAt);
