using FluentValidation;
using Core.Application.Requests;


namespace Core.Application.Validators;

public sealed class UpdateFinancialWorksheetRequestValidator : AbstractValidator<UpdateFinancialWorksheetRequest>
{
    public UpdateFinancialWorksheetRequestValidator()
    {
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Iban).NotEmpty().MaximumLength(64);
        RuleFor(x => x.PaymentSchedule).MaximumLength(2000);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}
