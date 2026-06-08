using Core.Application.DTOs;
using Core.Domain.Entities.Fund;

namespace Core.Application.Mappers;

public interface IFundCreditLimitDtoMapper
{
    FundCreditLimitDto Map(FundCreditLimit row, decimal utilized, string? lastSetByFullName);
}
