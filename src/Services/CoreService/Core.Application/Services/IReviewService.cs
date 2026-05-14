using BuildingBlocks.Application.Results;
using Services.CoreService.Core.Application.Contracts.Reviews;


namespace Services.CoreService.Core.Application.Services;

public interface IReviewService
{
    Task<Result> ApproveAsync(Guid caseId, ApprovePhaseRequest request, CancellationToken ct);
    Task<Result> RequestRevisionAsync(Guid caseId, RequestRevisionForPhaseRequest request, CancellationToken ct);
}
