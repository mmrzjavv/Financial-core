using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using FluentValidation;

namespace BuildingBlocks.Application.Validation;

public static class ValidationExtensions
{
    public static async Task<Result> ValidateAsync<T>(this IValidator<T> validator, T instance, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(instance, ct);
        if (validation.IsValid)
            return Result.Ok();

        var message = validation.ToErrorMessage();
        return Result.Fail(Error.Validation(message));
    }
}

