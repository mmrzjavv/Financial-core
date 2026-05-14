using FluentValidation;
using Services.CoreService.Core.Application.Contracts.Reviews;


namespace Services.CoreService.Core.Application.Validation;

public sealed class ApprovePhaseRequestValidator : AbstractValidator<ApprovePhaseRequest>
{
    public ApprovePhaseRequestValidator()
    {
        RuleFor(x => x.Phase).IsInEnum();
        RuleFor(x => x.Comment).MaximumLength(4000);
    }
}
