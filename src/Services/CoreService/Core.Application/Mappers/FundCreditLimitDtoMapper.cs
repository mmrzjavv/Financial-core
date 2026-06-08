using Core.Application.DTOs;
using Core.Domain.Entities.Fund;

namespace Core.Application.Mappers;

public sealed class FundCreditLimitDtoMapper : IFundCreditLimitDtoMapper
{
    public FundCreditLimitDto Map(FundCreditLimit row, decimal utilized, string? lastSetByFullName)
        => new(
            row.Id,
            row.ModuleType,
            row.CreditLimitWithCheck,
            row.PeriodStart,
            row.ExpiresAt,
            utilized,
            row.CreditLimitWithCheck - utilized,
            row.LastSetByUserId,
            lastSetByFullName,
            row.CreatedAt,
            row.UpdatedAt);
}
