namespace Core.Application.Requests;

public sealed record UpdateDataEntry1Request(
    string StartupTitle,
    string BusinessDescription,
    decimal RequestedAmount,
    int TeamSize,
    string? Website,
    string? Country,
    string? City);

