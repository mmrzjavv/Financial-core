using Core.Application.Contracts.DataEntry;
using FluentValidation;
using Services.CoreService.Core.Domain.Enums;

namespace Core.Application.Validation;

public sealed class DataEntry1UpsertRequestValidator : AbstractValidator<DataEntry1UpsertRequest>
{
    public DataEntry1UpsertRequestValidator()
    {
        RuleFor(x => x.BusinessStage)
            .Must(s => s is BusinessStage.Idea or BusinessStage.HasPrototype)
            .WithMessage("مرحله کسب‌وکار نامعتبر است.");
        RuleFor(x => x.RequestedAmount).GreaterThan(0);
    }
}
