using Services.CoreService.Core.Domain.Enums;

namespace Core.Application.DTOs;

public abstract record CaseWorkflowHistoryDto(
    Guid Id,
    Guid CaseId,
    CasePhase FromPhase,
    CasePhase ToPhase,
    CaseStatus FromStatus,
    CaseStatus ToStatus,
    DateTimeOffset CreatedAt);

public sealed record CaseWorkflowHistoryApplicantDto(
    Guid Id,
    Guid CaseId,
    CasePhase FromPhase,
    CasePhase ToPhase,
    CaseStatus FromStatus,
    CaseStatus ToStatus,
    DateTimeOffset CreatedAt)
    : CaseWorkflowHistoryDto(Id, CaseId, FromPhase, ToPhase, FromStatus, ToStatus, CreatedAt);

public sealed record CaseWorkflowHistoryInternalDto(
    Guid Id,
    Guid CaseId,
    CasePhase FromPhase,
    CasePhase ToPhase,
    CaseStatus FromStatus,
    CaseStatus ToStatus,
    string ChangedByUserId,
    string ActorRole,
    string Action,
    Guid CorrelationId,
    string? Comment,
    DateTimeOffset CreatedAt)
    : CaseWorkflowHistoryDto(Id, CaseId, FromPhase, ToPhase, FromStatus, ToStatus, CreatedAt);
