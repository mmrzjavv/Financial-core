using BuildingBlocks.Application.Results;
using Services.CoreService.Core.Application.Contracts.Comments;


namespace Services.CoreService.Core.Application.Services;

public interface ICommentService
{
    Task<Result> AddCommentAsync(Guid caseId, AddCommentRequest request, CancellationToken ct);
    Task<Result> RequestRevisionAsync(Guid caseId, AddCommentRequest request, CancellationToken ct);
}
