using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;

namespace Services.CoreService.Core.Domain.Entities;

public sealed class FinancialWorksheet : Entity<Guid>, IAuditableEntity
{
    private FinancialWorksheet()
    {
        BankName = default!;
        Iban = default!;
        PaymentSchedule = default!;
    }

    public FinancialWorksheet(
        Guid caseId,
        string bankName,
        string iban,
        decimal approvedAmount,
        string paymentSchedule,
        string? notes)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        BankName = bankName;
        Iban = iban;
        ApprovedAmount = approvedAmount;
        PaymentSchedule = paymentSchedule;
        Notes = notes;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid CaseId { get; private set; }
    public InvestmentCase Case { get; private set; } = default!;

    public string BankName { get; private set; }
    public string Iban { get; private set; }
    public decimal ApprovedAmount { get; private set; }
    public string PaymentSchedule { get; private set; }
    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Update(string bankName, string iban, decimal approvedAmount, string paymentSchedule, string? notes)
    {
        BankName = bankName;
        Iban = iban;
        ApprovedAmount = approvedAmount;
        PaymentSchedule = paymentSchedule;
        Notes = notes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
