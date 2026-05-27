using BuildingBlocks.Application.Results;
using Core.Application.DTOs;

namespace Core.Application.Abstractions;

public interface IKanbanAppService
{
    Task<Result<IReadOnlyList<KanbanCaseCardDto>>> GetActionRequiredAsync(CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<KanbanCaseCardDto>>> GetActionRequiredInvestmentOnlyAsync(CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<KanbanCaseSummaryDto>>> GetWatchingAsync(CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<KanbanCaseSummaryDto>>> GetWatchingInvestmentOnlyAsync(CancellationToken cancellationToken);
}
