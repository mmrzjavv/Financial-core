using Core.Domain.Enums;

namespace Core.Application.DTOs;

public sealed record CaseDocumentTypeVersionsDto(
    DocumentType DocumentType,
    CaseDocumentDto? Latest,
    IReadOnlyList<CaseDocumentDto> Versions);
