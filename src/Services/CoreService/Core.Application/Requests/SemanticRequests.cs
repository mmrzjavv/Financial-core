using Services.CoreService.Core.Domain.Enums;


namespace Core.Application.Requests;

public sealed record SemanticTransitionRequest(string? Comment);
public sealed record SemanticRevisionRequest(string Message);
public sealed record SemanticRejectRequest(string Reason);
public sealed record SemanticCancelRequest(string Reason);
public sealed record SemanticArchiveRequest(string Reason);

public sealed record AddCommentRequest(
    CasePhase Phase,
    string Message,
    bool IsInternal,
    Guid? ParentId = null);

public sealed record CaseEvaluationUpsertRequest(
    CasePhase Phase,
    string? Notes,
    IReadOnlyList<CaseEvaluationItemRequest> Items);

public sealed record CaseEvaluationItemRequest(string Title, bool IsApproved, string? Comment);