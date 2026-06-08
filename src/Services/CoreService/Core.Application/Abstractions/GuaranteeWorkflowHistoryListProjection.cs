using Core.Domain.Enums;

namespace Core.Application.Abstractions;

public sealed record GuaranteeWorkflowHistoryListProjection(
    Guid Id,
    GuaranteeCasePhase FromPhase,
    GuaranteeCasePhase ToPhase,
    GuaranteeCaseStatus FromStatus,
    GuaranteeCaseStatus ToStatus,
    string ChangedByUserId,
    string Action,
    string ActorRole,
    string? Comment,
    DateTimeOffset CreatedAt,
    string? ChangedByFullName);
