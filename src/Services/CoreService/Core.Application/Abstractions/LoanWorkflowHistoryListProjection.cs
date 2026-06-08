using Core.Domain.Enums;

namespace Core.Application.Abstractions;

public sealed record LoanWorkflowHistoryListProjection(
    Guid Id,
    LoanCasePhase FromPhase,
    LoanCasePhase ToPhase,
    LoanCaseStatus FromStatus,
    LoanCaseStatus ToStatus,
    string ChangedByUserId,
    string Action,
    string ActorRole,
    Guid CorrelationId,
    string? Comment,
    DateTimeOffset CreatedAt,
    string? ChangedByFullName);
