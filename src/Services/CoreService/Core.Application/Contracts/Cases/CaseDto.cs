using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Application.Contracts.Cases;

public sealed record CaseDto(
    Guid Id,
    string CaseNumber,
    string ApplicantUserId,
    ApplicantType ApplicantType,
    CasePhase CurrentPhase,
    CaseStatus CurrentStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? CompletedAt,
    uint RowVersion);

