using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Application.Contracts.Cases;

public sealed record CreateCaseRequest(ApplicantType ApplicantType);

