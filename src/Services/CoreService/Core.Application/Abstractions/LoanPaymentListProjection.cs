namespace Core.Application.Abstractions;

public sealed record LoanPaymentListProjection(
    Guid Id,
    decimal Amount,
    DateOnly PaymentDate,
    string TransactionNumber,
    string? ReceiptS3Key,
    string? Notes,
    int StageNumber,
    string CreatedByUserId,
    DateTimeOffset CreatedAt);
