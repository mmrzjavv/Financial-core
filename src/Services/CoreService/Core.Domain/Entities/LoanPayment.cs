using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;

namespace Core.Domain.Entities;

public sealed class LoanPayment : Entity<Guid>, IAuditableEntity
{
    private LoanPayment()
    {
        TransactionNumber = default!;
        CreatedByUserId = default!;
    }

    public LoanPayment(
        Guid caseId,
        decimal amount,
        DateOnly paymentDate,
        string transactionNumber,
        string? receiptS3Key,
        string? notes,
        int stageNumber,
        string createdByUserId)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        Amount = amount;
        PaymentDate = paymentDate;
        TransactionNumber = transactionNumber;
        ReceiptS3Key = receiptS3Key;
        Notes = notes;
        StageNumber = stageNumber;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid CaseId { get; private set; }
    public LoanCase Case { get; private set; } = default!;

    public decimal Amount { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public string TransactionNumber { get; private set; }
    public string? ReceiptS3Key { get; private set; }
    public string? Notes { get; private set; }
    public int StageNumber { get; private set; }
    public string CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Update(
        decimal amount,
        DateOnly paymentDate,
        string transactionNumber,
        string? receiptS3Key,
        string? notes,
        int stageNumber)
    {
        Amount = amount;
        PaymentDate = paymentDate;
        TransactionNumber = transactionNumber;
        ReceiptS3Key = receiptS3Key;
        Notes = notes;
        StageNumber = stageNumber;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
