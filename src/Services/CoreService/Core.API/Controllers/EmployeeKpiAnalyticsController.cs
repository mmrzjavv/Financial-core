using Asp.Versioning;
using BuildingBlocks.Application.Results;
using Core.API.Http;
using Core.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.API.Controllers;

/// <summary>Pre-aggregated employee SLA / KPI analytics for executive roles.</summary>
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/analytics")]
public sealed class EmployeeKpiAnalyticsController(
    IEmployeeKpiAnalyticsService analyticsService,
    IEmployeeKpiAggregationService aggregationService) : ApiControllerBase
{
    [HttpGet("employee-kpis")]
    [Authorize(Policy = "Analytics.EmployeeKpi")]
    public async Task<IActionResult> GetEmployeeKpis([FromQuery] string? period, CancellationToken ct)
    {
        if (!EmployeeKpiPeriodResolver.TryParse(period, out var parsedPeriod))
            return BadRequest(new { message = "Invalid period. Use Last30Days, Last90Days, ThisQuarter, or AllTime." });

        var result = await analyticsService.GetEmployeeKpisAsync(parsedPeriod, ct);
        return Respond(result, "Employee KPI analytics loaded.");
    }

    /// <summary>Manually runs the KPI aggregation job (urgent refresh). Executive roles only.</summary>
    [HttpPost("employee-kpis/run-job")]
    [Authorize(Policy = "Analytics.EmployeeKpi")]
    public async Task<IActionResult> RunEmployeeKpiJob(CancellationToken ct)
    {
        var computedAt = await aggregationService.AggregateAsync(ct);
        var payload = new EmployeeKpiJobRunResultDto
        {
            ComputedAtUtc = computedAt,
            Message = "محاسبه KPI پرسنل با موفقیت انجام شد."
        };
        return Respond(Result<EmployeeKpiJobRunResultDto>.Ok(payload), "Employee KPI job completed.");
    }

    [HttpPost("employee-kpis/refresh")]
    [Authorize(Policy = "Analytics.EmployeeKpi")]
    public Task<IActionResult> RefreshEmployeeKpis(CancellationToken ct) => RunEmployeeKpiJob(ct);
}
