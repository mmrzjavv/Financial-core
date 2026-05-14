using Services.CoreService.Core.Domain.Enums;


namespace Services.CoreService.Core.Application.Contracts.Documents;

public sealed record CreateUploadUrlRequest(
    DocumentType DocumentType,
    string FileName,
    string MimeType,
    long FileSize);
