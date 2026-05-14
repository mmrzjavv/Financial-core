using BuildingBlocks.Application.Results;
using Services.CoreService.Core.Application.Contracts.Documents;


namespace Services.CoreService.Core.Application.Services;

public interface IDocumentService
{
    Task<Result<PresignedUrlResponse>> CreatePresignedUploadUrlAsync(Guid caseId, CreateUploadUrlRequest request, CancellationToken ct);
    Task<Result> RegisterUploadedDocumentAsync(Guid caseId, RegisterUploadedDocumentRequest request, CancellationToken ct);
}
