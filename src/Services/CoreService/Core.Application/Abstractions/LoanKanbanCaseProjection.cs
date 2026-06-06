using Core.Domain.Enums;

namespace Core.Application.Abstractions;

public sealed record LoanKanbanCaseProjection(
    Guid Id,
    string CaseNumber,
    ApplicantType ApplicantType,
    LoanCasePhase CurrentPhase,
    LoanCaseStatus CurrentStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    decimal? RequestedAmount,
    string? CompanyName);
