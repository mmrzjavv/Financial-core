using BuildingBlocks.Application.Errors;

namespace BuildingBlocks.Application.Results;

public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    public static Result Ok() => new(true, null);
    public static Result Fail(Error error) => new(false, error);
}

public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, Error? error) : base(isSuccess, error) => Value = value;

    public T? Value { get; }

    public static Result<T> Ok(T value) => new(true, value, null);
    public static new Result<T> Fail(Error error) => new(false, default, error);
}

