namespace Services.CoreService.Core.Application.Contracts.Finance;

public sealed record FinancialWorksheetUpsertRequest(
    string BankName,
    string IBAN,
    decimal ApprovedAmount,
    string PaymentSchedule,
    string? Notes);

