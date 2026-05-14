using FluentValidation;
using Services.CoreService.Core.Application.Contracts.Cases;

namespace Services.CoreService.Core.Application.Validation;

public sealed class SubmitCaseRequestValidator : AbstractValidator<SubmitCaseRequest>
{
    public SubmitCaseRequestValidator()
    {
        RuleFor(x => x.Comment).MaximumLength(4000);
    }
}

