using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities.Loan;

public sealed class LoanInstallment : Entity<Guid>, IAuditableEntity
{
    private LoanInstallment()
    {
    }

    public LoanInstallment(
        Guid caseId,
        int rowNumber,
        DateOnly installmentDate,
        decimal principalAmount,
        decimal profitAmount,
        decimal totalAmount,
        decimal fundShareOfPrincipal,
        decimal fundShareOfProfit,
        decimal fundShareOfTotal,
        bool isGracePeriod)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        RowNumber = rowNumber;
        InstallmentDate = installmentDate;
        PrincipalAmount = principalAmount;
        ProfitAmount = profitAmount;
        TotalAmount = totalAmount;
        FundShareOfPrincipal = fundShareOfPrincipal;
        FundShareOfProfit = fundShareOfProfit;
        FundShareOfTotal = fundShareOfTotal;
        IsGracePeriod = isGracePeriod;
        IsPaid = false;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid CaseId { get; private set; }
    public LoanCase Case { get; private set; } = default!;

    public int RowNumber { get; private set; }
    public DateOnly InstallmentDate { get; private set; }
    public decimal PrincipalAmount { get; private set; }
    public decimal ProfitAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal FundShareOfPrincipal { get; private set; }
    public decimal FundShareOfProfit { get; private set; }
    public decimal FundShareOfTotal { get; private set; }
    public bool IsGracePeriod { get; private set; }
    public bool IsPaid { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? ReminderSentAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void MarkPaid(DateTimeOffset paidAt)
    {
        IsPaid = true;
        PaidAt = paidAt;
        UpdatedAt = paidAt;
    }

    public void MarkReminderSent(DateTimeOffset sentAt)
    {
        ReminderSentAt = sentAt;
        UpdatedAt = sentAt;
    }
}
