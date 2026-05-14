using System.Net;
using System.Runtime.CompilerServices;
using Serilog;

namespace Core.Application.Identity.Common.DTOs;

public class PanelOperationResult<T>
{
    public bool Success { get; private set; } = false;
    public string Message { get; private set; } = string.Empty;
    public DateTime OperationDate { get; private set; } = DateTime.Now;
    public HttpStatusCode Status { get; private set; }
    public T? Data { get; private set; }
    public List<T>? List { get; private set; }
    public int? TotalCount { get; private set; }
    public List<string>? ValidationErrors { get; private set; }
    public string? ExMessage { get; private set; }

    public PanelOperationResult<T> Succeed(
        string message,
        T? data = default,
        [CallerMemberName] string operationName = "")
    {
        Success = true;
        Message = message;
        Status = HttpStatusCode.OK;
        Data = data;

        LogOperation(operationName, "SUCCESS");
        return this;
    }

    public PanelOperationResult<T> Succeed(
        string message,
        List<T>? list,
        int? totalCount,
        [CallerMemberName] string operationName = "")
    {
        Success = true;
        Message = message;
        Status = HttpStatusCode.OK;
        List = list;
        TotalCount = totalCount;

        LogOperation(operationName, "SUCCESS");
        return this;
    }

    public PanelOperationResult<T> Failed(
        string message,
        HttpStatusCode status = HttpStatusCode.BadRequest,
        string? exMessage = null,
        [CallerMemberName] string operationName = "")
    {
        Success = false;
        Message = message;
        Status = status;
        ExMessage = exMessage;

        LogOperation(operationName, "FAILED");
        return this;
    }

    public PanelOperationResult<T> Failed(
        string message,
        List<string> validationErrors,
        HttpStatusCode status = HttpStatusCode.BadRequest,
        [CallerMemberName] string operationName = "")
    {
        Success = false;
        Message = message;
        Status = status;
        ValidationErrors = validationErrors;

        LogOperation(operationName, "FAILED");
        return this;
    }

    public PanelOperationResult<T> Failed(
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

        LogOperation(operationName, "FAILED");
        return this;
    }

    #region Logging

    private void LogOperation(string operationName, string resultType)
    {
        var statusCode = (int)Status;
        var hasObject = Data is not null;
        var listCount = List?.Count ?? 0;
        var total = TotalCount ?? 0;
        var exPart = !string.IsNullOrWhiteSpace(ExMessage) ? $" | Ex:{ExMessage}" : string.Empty;

        var template =
            "[Application][{OperationName}] {ResultType} | Status:{StatusCode} | Msg:{Message} | Obj:{HasObject} | List:{ListCount} | Total:{TotalCount}{ExPart}";

        if (resultType.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information(
                template,
                operationName,
                resultType,
                statusCode,
                Message,
                hasObject,
                listCount,
                total,
                exPart);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(ExMessage))
            {
                Log.Error(
                    template,
                    operationName,
                    resultType,
                    statusCode,
                    Message,
                    hasObject,
                    listCount,
                    total,
                    exPart);
            }
            else
            {
                Log.Warning(
                    template,
                    operationName,
                    resultType,
                    statusCode,
                    Message,
                    hasObject,
                    listCount,
                    total,
                    exPart);
            }
        }
    }
}

# endregion