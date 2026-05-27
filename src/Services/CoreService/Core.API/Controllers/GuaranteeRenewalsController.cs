using System.Net;
using Asp.Versioning;
using Core.API.Http;
using Core.Application.Abstractions;
using Core.Application.Common;
using Core.Application.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.API.Controllers;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/guarantee-renewals")]
public sealed class GuaranteeRenewalsController(IGuaranteeRenewalAppService service) : ApiControllerBase
{
    [HttpPost]
    [Authorize(Policy = "ApplicantOnly")]
    public async Task<IActionResult> Create([FromBody] CreateGuaranteeRenewalRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return Respond(result, GuaranteeSuccessMessages.RenewalCreated, HttpStatusCode.Created);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await service.GetAsync(id, ct);
        return Respond(result, GuaranteeSuccessMessages.GuaranteeCaseRetrieved);
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = "ApplicantOnly")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var result = await service.SubmitAsync(id, ct);
        return Respond(result, GuaranteeSuccessMessages.RenewalSubmitted, HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/ceo/approve")]
    [Authorize(Policy = "GuaranteeCases.CeoApprove")]
    public async Task<IActionResult> CeoApprove(Guid id, CancellationToken ct)
    {
        var result = await service.CeoApproveAsync(id, ct);
        return Respond(result, GuaranteeSuccessMessages.RenewalCeoApproved, HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/ceo/reject")]
    [Authorize(Policy = "GuaranteeCases.CeoApprove")]
    public async Task<IActionResult> CeoReject(Guid id, [FromBody] SemanticRevisionRequest request, CancellationToken ct)
    {
        var result = await service.CeoRejectAsync(id, request.Message, ct);
        return Respond(result, GuaranteeSuccessMessages.RenewalCeoRejected, HttpStatusCode.Accepted);
    }

    [HttpPut("{id:guid}/credit/dates")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> UpdateCreditDates(Guid id, [FromBody] UpdateGuaranteeRenewalDatesRequest request, CancellationToken ct)
    {
        var result = await service.UpdateCreditDatesAsync(id, request, ct);
        return Respond(result, GuaranteeSuccessMessages.RenewalDatesUpdated, HttpStatusCode.Accepted);
    }
}
