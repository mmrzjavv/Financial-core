using FluentValidation;
using Core.Application.Requests;
using Services.CoreService.Core.Domain.Enums;


namespace Core.Application.Validators;

public sealed class CreateInvestmentCaseRequestValidator : AbstractValidator<CreateInvestmentCaseRequest>
{
    public CreateInvestmentCaseRequestValidator()
    {
        RuleFor(x => x.ApplicantType).IsInEnum();

        When(x => x.ApplicantType == ApplicantType.Company, () =>
        {
            RuleFor(x => x.Company).NotNull();
            RuleFor(x => x.Company!.Name).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Company!.EconomicCode).NotEmpty().MaximumLength(32);
            RuleFor(x => x.Company!.RegistrationNumber).MaximumLength(64);
            RuleFor(x => x.Company!.NationalId).MaximumLength(64);
            RuleFor(x => x.Company!.PhoneNumber).MaximumLength(32);
            RuleFor(x => x.Company!.Address).MaximumLength(512);
            RuleFor(x => x.Company!.City).MaximumLength(128);
            RuleFor(x => x.Company!.Province).MaximumLength(128);
            RuleFor(x => x.Company!.PostalCode).MaximumLength(32);
        });
    }
}
