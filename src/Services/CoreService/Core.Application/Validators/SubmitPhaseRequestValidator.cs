using FluentValidation;
using Core.Application.Requests;

namespace Core.Application.Validators;

public sealed class SubmitPhaseRequestValidator : AbstractValidator<SubmitPhaseRequest>
{
    public SubmitPhaseRequestValidator() => RuleFor(x => x.Phase).IsInEnum();
}

