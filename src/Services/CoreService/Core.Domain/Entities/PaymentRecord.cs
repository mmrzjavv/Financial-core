using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities;

public sealed class PaymentRecord : Entity<Guid>, IAuditableEntity
{
    private PaymentRecord()
    {
        TransactionNumber = default!;
        CreatedByUserId = default!;
    }

    public PaymentRecord(
        Guid caseId,
        decimal amount,
        DateOnly paymentDate,
        string transactionNumber,
        string? receiptS3Key,
        string? notes,
        PaymentMethod method,
        PaymentStatus status,
        string createdByUserId)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        Amount = amount;
        PaymentDate = paymentDate;
        TransactionNumber = transactionNumber;
        ReceiptS3Key = receiptS3Key;
        Notes = notes;
        Method = method;
        Status = status;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid CaseId { get; private set; }
    public InvestmentCase Case { get; private set; } = default!;

    public decimal Amount { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public string TransactionNumber { get; private set; }
    public string? ReceiptS3Key { get; private set; }
    public string? Notes { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Update(
        decimal amount,
        DateOnly paymentDate,
        string transactionNumber,
        string? receiptS3Key,
        string? notes,
        PaymentMethod method,
        PaymentStatus status)
    {
        Amount = amount;
        PaymentDate = paymentDate;
        TransactionNumber = transactionNumber;
        ReceiptS3Key = receiptS3Key;
        Notes = notes;
        Method = method;
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

