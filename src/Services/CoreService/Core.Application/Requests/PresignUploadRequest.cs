using Core.Domain.Enums;

namespace Core.Application.Requests;

public sealed record PresignUploadRequest(DocumentType DocumentType, string FileName, string MimeType, long FileSize);