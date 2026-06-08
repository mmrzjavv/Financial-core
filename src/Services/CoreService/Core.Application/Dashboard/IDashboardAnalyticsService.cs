using BuildingBlocks.Application.Results;

namespace Core.Application.Dashboard;

public interface IDashboardAnalyticsService
{
    Task<Result<RoleDashboardResponse>> GetMyDashboardAsync(CancellationToken cancellationToken = default);

    Task<Result<CeoDashboardDto>> GetCeoDashboardAsync(CancellationToken cancellationToken = default);

    Task<Result<BoardDashboardDto>> GetBoardDashboardAsync(CancellationToken cancellationToken = default);

    Task<Result<DepartmentDashboardViewDto>> GetDepartmentDashboardAsync(
        string? departmentKey = null,
        CancellationToken cancellationToken = default);

    Task<Result<ApplicantDashboardViewDto>> GetApplicantDashboardAsync(CancellationToken cancellationToken = default);

    Task<Result<AdminDashboardOverviewDto>> GetAdminOverviewAsync(CancellationToken cancellationToken = default);
}
