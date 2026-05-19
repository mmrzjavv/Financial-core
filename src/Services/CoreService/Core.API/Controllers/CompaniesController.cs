using System.Net;
using Asp.Versioning;
using Core.API.Http;
using Core.Application.Common;
using Core.Application.Requests;
using Core.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.API.Controllers;

/// <summary>Applicant company profiles linked to cases.</summary>
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/identity/companies")]
[Authorize]
public sealed class CompaniesController(ICompanyAppService service) : ApiControllerBase
{
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
        => Respond(await service.GetMyCompaniesAsync(ct), CompanySuccessMessages.CompaniesRetrieved);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveCompanyRequest request, CancellationToken ct)
        => Respond(await service.CreateAsync(request, ct), CompanySuccessMessages.CompanyCreated, HttpStatusCode.Created);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveCompanyRequest request, CancellationToken ct)
        => Respond(await service.UpdateAsync(id, request, ct), CompanySuccessMessages.CompanyUpdated);
}
