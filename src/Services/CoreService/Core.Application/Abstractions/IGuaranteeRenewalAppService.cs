using BuildingBlocks.Application.Results;
using Core.Application.DTOs;
using Core.Application.Requests;

namespace Core.Application.Abstractions;

public interface IGuaranteeRenewalAppService
{
    Task<Result<GuaranteeRenewalDto>> CreateAsync(CreateGuaranteeRenewalRequest request, CancellationToken ct);
    Task<Result<GuaranteeRenewalDto>> GetAsync(Guid id, CancellationToken ct);
    Task<Result> SubmitAsync(Guid id, CancellationToken ct);
    Task<Result> CeoApproveAsync(Guid id, CancellationToken ct);
    Task<Result> CeoRejectAsync(Guid id, string reason, CancellationToken ct);
    Task<Result> UpdateCreditDatesAsync(Guid id, UpdateGuaranteeRenewalDatesRequest request, CancellationToken ct);
}
