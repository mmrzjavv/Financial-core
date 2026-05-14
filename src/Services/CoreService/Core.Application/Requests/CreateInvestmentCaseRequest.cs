using Services.CoreService.Core.Domain.Enums;

namespace Core.Application.Requests;

public sealed record CreateInvestmentCaseRequest(string ApplicantUserId, ApplicantType ApplicantType, CreateInvestmentCaseCompanyRequest? Company);

public sealed record CreateInvestmentCaseCompanyRequest(
    string Name,
    string EconomicCode,
    string? RegistrationNumber,
    string? NationalId,
    string? PhoneNumber,
    string? Address,
    string? City,
    string? Province,
    string? PostalCode);
