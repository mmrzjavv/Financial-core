using System.Net;
using System.Runtime.CompilerServices;
using BuildingBlocks.Application.Errors;

namespace BuildingBlocks.Application.Results;

public class ApiOperationResult<T>
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTime OperationDate { get; private set; } = DateTime.UtcNow;
    public HttpStatusCode Status { get; private set; }
    public T? Data { get; private set; }
    public List<T>? List { get; private set; }
    public int? TotalCount { get; private set; }
    public List<string>? ValidationErrors { get; private set; }
    public string? ExMessage { get; private set; }

    public ApiOperationResult<T> Succeed(
        string message,
        T? data = default,
        HttpStatusCode status = HttpStatusCode.OK,
        [CallerMemberName] string operationName = "")
    {
        Success = true;
        Message = message;
        Status = status;
        Data = data;
        OperationDate = DateTime.UtcNow;
        return this;
    }

    public ApiOperationResult<T> Succeed(
        string message,
        List<T>? list,
        int? totalCount,
        HttpStatusCode status = HttpStatusCode.OK,
        [CallerMemberName] string operationName = "")
    {
        Success = true;
        Message = message;
        Status = status;
        List = list;
        TotalCount = totalCount;
        OperationDate = DateTime.UtcNow;
        return this;
    }

    public ApiOperationResult<T> Failed(
        string message,
        HttpStatusCode status = HttpStatusCode.BadRequest,
        string? exMessage = null,
        [CallerMemberName] string operationName = "")
    {
        Success = false;
        Message = message;
        Status = status;
        ExMessage = exMessage;
        OperationDate = DateTime.UtcNow;
        return this;
    }

    public ApiOperationResult<T> Failed(
        string message,
        List<string> validationErrors,
        HttpStatusCode status = HttpStatusCode.BadRequest,
        [CallerMemberName] string operationName = "")
    {
        Success = false;
        Message = message;
        Status = status;
        ValidationErrors = validationErrors;
        OperationDate = DateTime.UtcNow;
        return this;
    }

    public ApiOperationResult<T> Failed(
        string message,
        IDictionary<string, string[]>? validationErrors,
        HttpStatusCode status = HttpStatusCode.BadRequest,
        [CallerMemberName] string operationName = "")
    {
        Success = false;
        Message = message;
        Status = status;
        ValidationErrors = validationErrors is null
            ? new List<string>()
            : validationErrors
                .SelectMany(pair => pair.Value.Select(error => $"{pair.Key}: {error}"))
                .ToList();
        OperationDate = DateTime.UtcNow;
        return this;
    }
}
