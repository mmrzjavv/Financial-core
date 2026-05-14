using BuildingBlocks.Application.Results;
using Services.CoreService.Core.Application.Contracts.Payments;


namespace Services.CoreService.Core.Application.Services;

public interface IPaymentService
{
    Task<Result> RecordAsync(Guid caseId, RecordPaymentRequest request, CancellationToken ct);
}
