namespace Core.Application.Requests;

public sealed record SaveCompanyRequest(
    string Name,
    string EconomicCode,
    string? RegistrationNumber,
    string? NationalId,
    string? PhoneNumber,
    string? Address,
    string? City,
    string? Province,
    string? PostalCode);

public sealed record CompanyDto(
    Guid Id,
    string Name,
    string EconomicCode,
    string? RegistrationNumber,
    string? NationalId,
    string? PhoneNumber,
    string? Address,
    string? City,
    string? Province,
    string? PostalCode);
