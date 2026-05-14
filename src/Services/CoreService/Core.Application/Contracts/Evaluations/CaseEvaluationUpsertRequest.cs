using Services.CoreService.Core.Domain.Enums;


namespace Core.Application.Contracts.Evaluations;

public sealed record CaseEvaluationUpsertRequest(
    CasePhase Phase,
    string? Notes,
    IReadOnlyList<CaseEvaluationItemRequest> Items);

public sealed record CaseEvaluationItemRequest(string Title, bool IsApproved, string? Comment);
