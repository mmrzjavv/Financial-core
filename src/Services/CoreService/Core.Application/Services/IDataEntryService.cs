using BuildingBlocks.Application.Results;
using Services.CoreService.Core.Application.Contracts.DataEntry;


namespace Services.CoreService.Core.Application.Services;

public interface IDataEntryService
{
    Task<Result> UpsertDataEntry1Async(Guid caseId, DataEntry1UpsertRequest request, CancellationToken ct);
    Task<Result> UpsertDataEntry2Async(Guid caseId, DataEntry2UpsertRequest request, CancellationToken ct);
}
