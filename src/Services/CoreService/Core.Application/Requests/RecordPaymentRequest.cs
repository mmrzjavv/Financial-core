using Core.Domain.Enums;

namespace Core.Application.Requests;

public sealed record RecordPaymentRequest(
    decimal Amount,
    DateOnly PaymentDate,
    string TransactionNumber,
    string? ReceiptS3Key,
    string? Notes,
    PaymentMethod Method,
    PaymentStatus Status);

public sealed record UpdatePaymentRequest(
    decimal Amount,
    DateOnly PaymentDate,
    string TransactionNumber,
    string? ReceiptS3Key,
    string? Notes,
    PaymentMethod Method,
    PaymentStatus Status);