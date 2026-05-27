using Asp.Versioning;
using Core.API.Http;
using Core.Application.Abstractions;
using Core.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.API.Controllers;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/kanban")]
public sealed class KanbanController(IKanbanAppService kanbanService) : ApiControllerBase
{
    [HttpGet("action-required")]
    [Authorize]
    public async Task<IActionResult> GetActionRequired(CancellationToken ct)
    {
        var result = await kanbanService.GetActionRequiredAsync(ct);
        return Respond(result, GuaranteeSuccessMessages.KanbanActionRequiredRetrieved);
    }

    [HttpGet("watching")]
    [Authorize]
    public async Task<IActionResult> GetWatching(CancellationToken ct)
    {
        var result = await kanbanService.GetWatchingAsync(ct);
        return Respond(result, GuaranteeSuccessMessages.KanbanWatchingRetrieved);
    }
}
