namespace Core.Application.DTOs;

public sealed record DataEntry1Dto(
    string StartupTitle,
    string BusinessDescription,
    decimal RequestedAmount,
    int TeamSize,
    string? Website,
    string? Country,
    string? City);
