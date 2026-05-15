using Services.CoreService.Core.Domain.Enums;

namespace Core.Application.DTOs;

public sealed record KanbanCaseCardDto(
    Guid Id,
    string CaseNumber,
    CasePhase CurrentPhase,
    string PhaseTitle,
    CaseStatus CurrentStatus,
    string StatusTitle,
    ApplicantType ApplicantType,
    string? StartupTitle,
    string? CompanyName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string PendingActionLabel,
    IReadOnlyList<string> AllowedActions);

public sealed record KanbanCaseSummaryDto(
    Guid Id,
    string CaseNumber,
    CasePhase CurrentPhase,
    string PhaseTitle,
    CaseStatus CurrentStatus,
    string StatusTitle,
    string? StartupTitle,
    DateTimeOffset CreatedAt,
    string PendingActionLabel);
