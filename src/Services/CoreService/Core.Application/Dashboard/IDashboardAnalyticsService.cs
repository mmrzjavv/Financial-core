using BuildingBlocks.Application.Results;

namespace Core.Application.Dashboard;

public interface IDashboardAnalyticsService
{
    Task<Result<RoleDashboardResponse>> GetMyDashboardAsync(CancellationToken cancellationToken = default);

    Task<Result<CeoDashboardDto>> GetCeoDashboardAsync(CancellationToken cancellationToken = default);

    Task<Result<BoardDashboardDto>> GetBoardDashboardAsync(CancellationToken cancellationToken = default);

    Task<Result<DepartmentDashboardViewDto>> GetDepartmentDashboardAsync(CancellationToken cancellationToken = default);

    Task<Result<ApplicantDashboardViewDto>> GetApplicantDashboardAsync(CancellationToken cancellationToken = default);
}
