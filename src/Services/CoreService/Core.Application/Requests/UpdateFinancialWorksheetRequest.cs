namespace Core.Application.Requests;

public sealed record UpdateFinancialWorksheetRequest(
    string BankName,
    string Iban,
    decimal? ApprovedAmount,
    string? PaymentSchedule,
    string? Notes);

