using BuildingBlocks.Application.Results;

namespace Core.Application.Dashboard;

public interface IExecutiveDashboardService
{
    Task<Result<CeoDashboardDto>> GetCeoDashboardAsync(CancellationToken cancellationToken = default);
    Task<Result<BoardDashboardDto>> GetBoardDashboardAsync(CancellationToken cancellationToken = default);
}
