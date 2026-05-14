namespace Services.CoreService.Core.Application.Contracts.DataEntry;

public sealed record DataEntry1UpsertRequest(
    string StartupTitle,
    string BusinessDescription,
    decimal RequestedAmount,
    int TeamSize,
    string? Website,
    string? Country,
    string? City,
    string? Industry);

