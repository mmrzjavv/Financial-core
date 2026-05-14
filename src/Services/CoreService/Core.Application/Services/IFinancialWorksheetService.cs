using BuildingBlocks.Application.Results;
using Services.CoreService.Core.Application.Contracts.Finance;

namespace Services.CoreService.Core.Application.Services;

public interface IFinancialWorksheetService
{
    Task<Result> UpsertAsync(Guid caseId, FinancialWorksheetUpsertRequest request, CancellationToken ct);
}

