using FluentValidation;
using Services.CoreService.Core.Application.Contracts.Cases;


namespace Services.CoreService.Core.Application.Validation;

public sealed class CreateCaseRequestValidator : AbstractValidator<CreateCaseRequest>
{
    public CreateCaseRequestValidator()
    {
        RuleFor(x => x.ApplicantType).IsInEnum();
    }
}
