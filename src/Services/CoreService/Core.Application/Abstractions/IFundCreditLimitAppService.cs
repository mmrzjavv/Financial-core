using BuildingBlocks.Application.Results;
using Core.Application.DTOs;
using Core.Application.Requests;

namespace Core.Application.Abstractions;

public interface IFundCreditLimitAppService
{
    Task<Result<FundCreditLimitDto>> CreateAsync(CreateFundCreditLimitRequest request, CancellationToken ct);

    Task<Result<IReadOnlyList<FundCreditLimitDto>>> ListAsync(CancellationToken ct);

    Task<Result<FundCreditLimitDashboardSectionDto>> GetDashboardSectionAsync(CancellationToken ct);
}
