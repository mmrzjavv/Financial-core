using Core.Domain.Enums;

namespace Core.Application.DTOs;

public abstract record CaseCommentDto(
    Guid Id,
    Guid CaseId,
    CasePhase Phase,
    string Message,
    bool IsRevisionRequest,
    Guid? ParentId,
    IEnumerable<CaseCommentAttachmentDto> Attachments,
    DateTimeOffset CreatedAt);

public sealed record CaseCommentApplicantDto(
    Guid Id,
    Guid CaseId,
    CasePhase Phase,
    string Message,
    bool IsRevisionRequest,
    Guid? ParentId,
    IEnumerable<CaseCommentAttachmentDto> Attachments,
    DateTimeOffset CreatedAt)
    : CaseCommentDto(Id, CaseId, Phase, Message, IsRevisionRequest, ParentId, Attachments, CreatedAt);

public sealed record CaseCommentInternalDto(
    Guid Id,
    Guid CaseId,
    CasePhase Phase,
    string SenderUserId,
    string? SenderFullName,
    string? SenderRole,
    string Message,
    bool IsRevisionRequest,
    bool IsInternal,
    Guid? ParentId,
    IEnumerable<CaseCommentAttachmentDto> Attachments,
    DateTimeOffset CreatedAt)
    : CaseCommentDto(Id, CaseId, Phase, Message, IsRevisionRequest, ParentId, Attachments, CreatedAt);

public abstract record CaseCommentAttachmentDto(
    Guid Id,
    string FileName);

public sealed record CaseCommentAttachmentApplicantDto(
    Guid Id,
    string FileName)
    : CaseCommentAttachmentDto(Id, FileName);

public sealed record CaseCommentAttachmentInternalDto(
    Guid Id,
    string S3Key,
    string FileName)
    : CaseCommentAttachmentDto(Id, FileName);