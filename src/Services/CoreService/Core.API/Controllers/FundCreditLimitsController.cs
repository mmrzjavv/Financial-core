using Asp.Versioning;
using BuildingBlocks.Application.Results;
using Core.API.Http;
using Core.Application.Abstractions;
using Core.Application.Common;
using Core.Application.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.API.Controllers;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/fund-credit-limits")]
[Authorize(Policy = "FundCreditLimits.Manage")]
public sealed class FundCreditLimitsController(IFundCreditLimitAppService service) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFundCreditLimitRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return Respond(result, "سقف اعتبار دوره‌ای صندوق ثبت شد.", System.Net.HttpStatusCode.Created);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await service.ListAsync(ct);
        return Respond(result, "فهرست سقف‌های اعتبار دوره‌ای صندوق دریافت شد.");
    }
}
