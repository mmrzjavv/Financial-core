using Services.CoreService.Core.Domain.Enums;


namespace Core.Application.Contracts.Comments;

public sealed record AddCommentRequest(
    CasePhase Phase,
    string Message,
    bool IsInternal,
    Guid? ParentId = null);
