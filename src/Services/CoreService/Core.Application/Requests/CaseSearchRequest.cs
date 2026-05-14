using Services.CoreService.Core.Domain.Enums;

namespace Core.Application.Requests;

public sealed record CaseSearchRequest(
    string? CaseNumber,
    string? ApplicantUserId,
    CasePhase? Phase,
    CaseStatus? Status,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    int Page = 1,
    int PageSize = 10);