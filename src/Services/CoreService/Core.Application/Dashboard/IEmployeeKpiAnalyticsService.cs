using BuildingBlocks.Application.Results;

namespace Core.Application.Dashboard;

public interface IEmployeeKpiAnalyticsService
{
    Task<Result<EmployeeKpiResponseDto>> GetEmployeeKpisAsync(
        EmployeeKpiPeriod period,
        CancellationToken cancellationToken = default);
}
