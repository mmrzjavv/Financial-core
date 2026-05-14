using FluentValidation;
using Services.CoreService.Core.Application.Contracts.Evaluations;


namespace Services.CoreService.Core.Application.Validation;

public sealed class CaseEvaluationUpsertRequestValidator : AbstractValidator<CaseEvaluationUpsertRequest>
{
    public CaseEvaluationUpsertRequestValidator()
    {
        RuleFor(x => x.Phase).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(8000);

        RuleFor(x => x.Items).NotNull().NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new CaseEvaluationItemRequestValidator());
    }

    private sealed class CaseEvaluationItemRequestValidator : AbstractValidator<CaseEvaluationItemRequest>
    {
        public CaseEvaluationItemRequestValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Comment).MaximumLength(4000);
        }
    }
}
