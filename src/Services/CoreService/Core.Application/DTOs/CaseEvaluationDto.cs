using Services.CoreService.Core.Domain.Enums;


namespace Core.Application.DTOs;

public sealed record CaseEvaluationDto(
    Guid Id,
    Guid CaseId,
    CasePhase Phase,
    string ReviewerUserId,
    string ReviewerRole,
    string? Notes,
    IEnumerable<CaseEvaluationItemDto> Items,
    DateTimeOffset CreatedAt);

public sealed record CaseEvaluationItemDto(
    Guid Id,
    string Title,
    bool IsApproved,
    string? Comment);