using BuildingBlocks.Application.Results;
using BuildingBlocks.Contracts.Paging;
using Services.CoreService.Core.Application.Contracts.Cases;


namespace Services.CoreService.Core.Application.Services;

public interface ICaseService
{
    Task<Result<CaseDto>> CreateAsync(CreateCaseRequest request, CancellationToken ct);
    Task<Result<CaseDto>> GetAsync(Guid caseId, CancellationToken ct);
    Task<Result<PagedResult<CaseDto>>> ListMyCasesAsync(PagedRequest request, CancellationToken ct);
    Task<Result> SubmitAsync(Guid caseId, SubmitCaseRequest request, CancellationToken ct);
}
