using FluentValidation;
using Core.Application.Requests;


namespace Core.Application.Validators;

public sealed class RequestRevisionRequestValidator : AbstractValidator<RequestRevisionRequest>
{
    public RequestRevisionRequestValidator()
    {
        RuleFor(x => x.Phase).IsInEnum();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(8000);
    }
}
