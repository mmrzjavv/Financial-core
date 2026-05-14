using FluentValidation;
using Services.CoreService.Core.Application.Contracts.Reviews;

namespace Services.CoreService.Core.Application.Validation;

public sealed class RequestRevisionForPhaseRequestValidator : AbstractValidator<RequestRevisionForPhaseRequest>
{
    public RequestRevisionForPhaseRequestValidator()
    {
        RuleFor(x => x.Phase).IsInEnum();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(8000);
    }
}

