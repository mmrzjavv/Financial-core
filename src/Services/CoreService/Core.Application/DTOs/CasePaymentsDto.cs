using Core.Domain.Enums;

namespace Core.Application.DTOs;

public sealed record PaymentRecordDto(
    Guid Id,
    decimal Amount,
    DateOnly PaymentDate,
    string TransactionNumber,
    string? ReceiptS3Key,
    string? Notes,
    PaymentMethod Method,
    PaymentStatus Status,
    DateTimeOffset CreatedAt,
    string CreatedByUserId);

public sealed record CasePaymentsSummaryDto(
    decimal? ApprovedAmount,
    decimal TotalRecorded,
    decimal TotalConfirmed,
    decimal RemainingToComplete);

public sealed record CasePaymentsDto(
    IReadOnlyList<PaymentRecordDto> Payments,
    CasePaymentsSummaryDto Summary);
