namespace Services.CoreService.Core.Application.Contracts.Payments;

public sealed record RecordPaymentRequest(
    decimal Amount,
    DateOnly PaymentDate,
    string TransactionNumber,
    string? ReceiptS3Key,
    string? Notes);

