using FluentValidation;
using Core.Application.Requests;


namespace Core.Application.Validators;

public sealed class UpdateDataEntry1RequestValidator : AbstractValidator<UpdateDataEntry1Request>
{
    public UpdateDataEntry1RequestValidator()
    {
        RuleFor(x => x.StartupTitle).NotEmpty().MaximumLength(256);
        RuleFor(x => x.BusinessDescription).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.RequestedAmount).GreaterThan(0);
        RuleFor(x => x.TeamSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(5000);
        RuleFor(x => x.Website).MaximumLength(512);
        RuleFor(x => x.Country).MaximumLength(128);
        RuleFor(x => x.City).MaximumLength(128);
    }
}
