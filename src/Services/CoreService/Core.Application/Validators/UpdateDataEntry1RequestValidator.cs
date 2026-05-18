using Core.Application.Requests;
using Core.Domain.Enums;
using FluentValidation;

namespace Core.Application.Validators;

public sealed class UpdateDataEntry1RequestValidator : AbstractValidator<UpdateDataEntry1Request>
{
    public UpdateDataEntry1RequestValidator()
    {
        RuleFor(x => x.BusinessStage)
            .Must(s => s is BusinessStage.Idea or BusinessStage.HasPrototype)
            .WithMessage("مرحله کسب‌وکار نامعتبر است.");
        RuleFor(x => x.RequestedAmount).GreaterThan(0);
    }
}
