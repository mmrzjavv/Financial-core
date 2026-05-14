using FluentValidation;
using Services.CoreService.Core.Application.Contracts.DataEntry;


namespace Services.CoreService.Core.Application.Validation;

public sealed class DataEntry1UpsertRequestValidator : AbstractValidator<DataEntry1UpsertRequest>
{
    public DataEntry1UpsertRequestValidator()
    {
        RuleFor(x => x.StartupTitle).NotEmpty().MaximumLength(256);
        RuleFor(x => x.BusinessDescription).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.RequestedAmount).GreaterThan(0);
        RuleFor(x => x.TeamSize).GreaterThan(0);
        RuleFor(x => x.Website).MaximumLength(512);
        RuleFor(x => x.Country).MaximumLength(128);
        RuleFor(x => x.City).MaximumLength(128);
        RuleFor(x => x.Industry).MaximumLength(128);
    }
}
