using BuildingBlocks.Application.Results;
using BuildingBlocks.Contracts.Paging;
using Services.CoreService.Core.Application.Contracts.Evaluations;


namespace Services.CoreService.Core.Application.Services;

public interface IEvaluationService
{
    Task<Result> UpsertAsync(Guid caseId, CaseEvaluationUpsertRequest request, CancellationToken ct);
    Task<Result<PagedResult<CaseEvaluationUpsertRequest>>> ListAsync(Guid caseId, PagedRequest request, CancellationToken ct);
}
