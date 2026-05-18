using Core.Domain.Enums;

namespace Core.Application.DTOs;

public abstract record CaseDocumentDto(
    Guid Id,
    Guid CaseId,
    string FileName,
    string MimeType,
    long FileSize,
    int Version,
    DocumentType DocumentType,
    DateTimeOffset UploadedAt);

public sealed record CaseDocumentApplicantDto(
    Guid Id,
    Guid CaseId,
    string FileName,
    string MimeType,
    long FileSize,
    int Version,
    DocumentType DocumentType,
    DateTimeOffset UploadedAt)
    : CaseDocumentDto(Id, CaseId, FileName, MimeType, FileSize, Version, DocumentType, UploadedAt);

public sealed record CaseDocumentInternalDto(
    Guid Id,
    Guid CaseId,
    string S3Key,
    string FileName,
    string MimeType,
    long FileSize,
    int Version,
    DocumentType DocumentType,
    string UploadedByUserId,
    DateTimeOffset UploadedAt)
    : CaseDocumentDto(Id, CaseId, FileName, MimeType, FileSize, Version, DocumentType, UploadedAt);