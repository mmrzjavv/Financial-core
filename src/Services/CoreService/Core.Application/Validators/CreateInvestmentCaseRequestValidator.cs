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
            RuleFor(x => x.CompanyId).NotEmpty();
        });
    }
}
