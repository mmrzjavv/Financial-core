using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Application.Contracts.Documents;

public sealed record RegisterUploadedDocumentRequest(
    DocumentType DocumentType,
    string S3Key,
    string FileName,
    string MimeType,
    long FileSize,
    int Version);

