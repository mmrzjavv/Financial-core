using System.Net;
using BuildingBlocks.Application.Results;
using Core.Application.Requests;
using Microsoft.AspNetCore.Mvc;

namespace Core.API.Http;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Reads optional JSON body; empty POST without Content-Type is treated as default.</summary>
    protected async Task<SemanticTransitionRequest> ReadTransitionRequestAsync(CancellationToken cancellationToken)
    {
        if (Request.ContentLength is null or 0)
            return new SemanticTransitionRequest();

        if (Request.ContentType is null ||
            !Request.ContentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            return new SemanticTransitionRequest();

        return await Request.ReadFromJsonAsync<SemanticTransitionRequest>(cancellationToken)
               ?? new SemanticTransitionRequest();
    }

    protected IActionResult Respond<T>(ApiOperationResult<T> envelope)
        => ApiResponse.Send(envelope);

    protected IActionResult Respond<T>(Result<T> result, string successMessage, HttpStatusCode successStatus = HttpStatusCode.OK)
        => ApiResponse.From(result, successMessage, successStatus);

    protected IActionResult Respond(Result result, string successMessage, HttpStatusCode successStatus = HttpStatusCode.OK)
        => ApiResponse.From(result, successMessage, successStatus);
}
