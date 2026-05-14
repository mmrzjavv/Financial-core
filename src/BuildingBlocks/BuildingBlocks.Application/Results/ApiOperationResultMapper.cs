using System.Net;
using BuildingBlocks.Application.Errors;

namespace BuildingBlocks.Application.Results;

public static class ApiOperationResultMapper
{
    public static ApiOperationResult<T> ToApiOperationResult<T>(
        this Result<T> result,
        string successMessage,
        HttpStatusCode successStatus = HttpStatusCode.OK)
    {
        if (result.IsSuccess)
            return new ApiOperationResult<T>().Succeed(successMessage, result.Value, successStatus);

        return ToFailure<T>(result.Error!);
    }

    public static ApiOperationResult<object?> ToApiOperationResult(
        this Result result,
        string successMessage,
        HttpStatusCode successStatus = HttpStatusCode.OK)
    {
        if (result.IsSuccess)
            return new ApiOperationResult<object?>().Succeed(successMessage, null, successStatus);

        return ToFailure<object?>(result.Error!);
    }

    public static ApiOperationResult<T> ToFailure<T>(Error error)
    {
        var envelope = new ApiOperationResult<T>();
        return envelope.Failed(error.Message, MapStatus(error), exMessage: error.Code);
    }

    public static HttpStatusCode MapStatus(Error error) =>
        error.Code switch
        {
            "validation_error" => HttpStatusCode.BadRequest,
            "unauthorized" => HttpStatusCode.Unauthorized,
            "not_found" => HttpStatusCode.NotFound,
            "forbidden" => HttpStatusCode.Forbidden,
            "conflict" => HttpStatusCode.Conflict,
            _ => HttpStatusCode.InternalServerError
        };
}
