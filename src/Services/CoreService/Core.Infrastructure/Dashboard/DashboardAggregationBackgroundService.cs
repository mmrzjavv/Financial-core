using Core.Application.Dashboard;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Dashboard;

public sealed class DashboardAggregationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<DashboardAggregationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = configuration.GetValue("Dashboard:AggregationEnabled", true);
        if (!enabled)
        {
            logger.LogInformation("Dashboard aggregation background service is disabled.");
            return;
        }

        var intervalHours = configuration.GetValue("Dashboard:AggregationIntervalHours", 4);
        var interval = TimeSpan.FromHours(Math.Max(1, intervalHours));

        await RunAggregationCycleAsync(stoppingToken);

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunAggregationCycleAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Dashboard aggregation cycle failed");
            }
        }
    }

    private async Task RunAggregationCycleAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Dashboard aggregation cycle starting");
        using var scope = scopeFactory.CreateScope();
        var aggregation = scope.ServiceProvider.GetRequiredService<IDashboardAggregationService>();
        await aggregation.AggregateAllAsync(stoppingToken);
        logger.LogInformation("Dashboard aggregation cycle finished");
    }
}
