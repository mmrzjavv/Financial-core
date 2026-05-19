using Core.Application.Requests;
using FluentValidation;

namespace Core.Application.Validation;

public sealed class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentDate).NotEmpty();
        RuleFor(x => x.TransactionNumber).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ReceiptS3Key).MaximumLength(512);
        RuleFor(x => x.Notes).MaximumLength(4000);
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
    }
}
