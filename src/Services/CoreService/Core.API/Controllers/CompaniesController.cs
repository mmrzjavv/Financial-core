using System.Net;
using Asp.Versioning;
using Core.API.Http;
using Core.Application.Requests;
using Core.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/panel/companies")]
[Authorize]
public sealed class CompaniesController(ICompanyAppService service) : ControllerBase
{
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await service.GetMyCompaniesAsync(ct);
        return ApiResponse.From(result, "فهرست شرکت‌ها دریافت شد.");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveCompanyRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return ApiResponse.From(result, "شرکت ثبت شد.", HttpStatusCode.Created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveCompanyRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        return ApiResponse.From(result, "اطلاعات شرکت به‌روزرسانی شد.");
    }
}
