using FluentValidation;
using Core.Application.Requests;

namespace Core.Application.Validators;

public sealed class SaveCompanyRequestValidator : AbstractValidator<SaveCompanyRequest>
{
    public SaveCompanyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.EconomicCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.RegistrationNumber).MaximumLength(64);
        RuleFor(x => x.NationalId).MaximumLength(64);
        RuleFor(x => x.PhoneNumber).MaximumLength(32);
        RuleFor(x => x.Address).MaximumLength(1024);
        RuleFor(x => x.City).MaximumLength(128);
        RuleFor(x => x.Province).MaximumLength(128);
        RuleFor(x => x.PostalCode).MaximumLength(32);
    }
}
