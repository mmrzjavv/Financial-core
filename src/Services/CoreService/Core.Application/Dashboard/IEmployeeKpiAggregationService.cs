namespace Core.Application.Dashboard;

public interface IEmployeeKpiAggregationService
{
    Task<DateTimeOffset> AggregateAsync(CancellationToken cancellationToken = default);
}
