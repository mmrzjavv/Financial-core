using Asp.Versioning;
using Core.API.Http;
using Core.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.API.Controllers;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/dashboard")]
public sealed class DashboardController(IExecutiveDashboardService dashboardService) : ApiControllerBase
{
    [HttpGet("ceo")]
    [Authorize(Policy = "Dashboard.Ceo")]
    public async Task<IActionResult> GetCeoDashboard(CancellationToken ct)
    {
        var result = await dashboardService.GetCeoDashboardAsync(ct);
        return Respond(result, "داشبورد مدیرعامل بارگذاری شد.");
    }

    [HttpGet("board")]
    [Authorize(Policy = "Dashboard.Board")]
    public async Task<IActionResult> GetBoardDashboard(CancellationToken ct)
    {
        var result = await dashboardService.GetBoardDashboardAsync(ct);
        return Respond(result, "داشبورد هیئت مدیره بارگذاری شد.");
    }
}
