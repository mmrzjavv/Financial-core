namespace Core.Application.Dashboard;

public interface IDashboardAggregationService
{
    Task AggregateAllAsync(CancellationToken cancellationToken = default);
}
