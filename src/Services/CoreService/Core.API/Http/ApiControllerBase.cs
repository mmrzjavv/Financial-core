using System.Net;
using BuildingBlocks.Application.Results;
using Microsoft.AspNetCore.Mvc;

namespace Core.API.Http;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult Respond<T>(ApiOperationResult<T> envelope)
        => ApiResponse.Send(envelope);

    protected IActionResult Respond<T>(Result<T> result, string successMessage, HttpStatusCode successStatus = HttpStatusCode.OK)
        => ApiResponse.From(result, successMessage, successStatus);

    protected IActionResult Respond(Result result, string successMessage, HttpStatusCode successStatus = HttpStatusCode.OK)
        => ApiResponse.From(result, successMessage, successStatus);
}
