using FluentValidation;
using Services.CoreService.Core.Application.Contracts.Payments;

namespace Services.CoreService.Core.Application.Validation;

public sealed class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.TransactionNumber).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ReceiptS3Key).MaximumLength(512);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}

