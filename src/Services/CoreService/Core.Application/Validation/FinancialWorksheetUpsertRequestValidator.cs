using FluentValidation;
using Services.CoreService.Core.Application.Contracts.Finance;


namespace Services.CoreService.Core.Application.Validation;

public sealed class FinancialWorksheetUpsertRequestValidator : AbstractValidator<FinancialWorksheetUpsertRequest>
{
    public FinancialWorksheetUpsertRequestValidator()
    {
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.IBAN).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ApprovedAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaymentSchedule).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}
