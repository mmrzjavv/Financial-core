using System.Net;
using BuildingBlocks.Application.Results;
using Microsoft.AspNetCore.Mvc;

namespace Core.API.Http;

public static class ApiResponse
{
    public static IActionResult Send<T>(ApiOperationResult<T> envelope) =>
        new ObjectResult(envelope) { StatusCode = (int)envelope.Status };

    public static IActionResult From<T>(
        Result<T> result,
        string successMessage,
        HttpStatusCode successStatus = HttpStatusCode.OK) =>
        Send(result.ToApiOperationResult(successMessage, successStatus));

    public static IActionResult From(
        Result result,
        string successMessage,
        HttpStatusCode successStatus = HttpStatusCode.OK) =>
        Send(result.ToApiOperationResult(successMessage, successStatus));
}
