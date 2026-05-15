using Core.Application.Requests;
using Services.CoreService.Core.Domain.Enums;


namespace Core.Application.DTOs;

public abstract record InvestmentCaseDto(
    Guid Id,
    string CaseNumber,
    ApplicantType ApplicantType,
    CasePhase CurrentPhase,
    CaseStatus CurrentStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? CompletedAt);

public sealed record InvestmentCaseApplicantDto(
    Guid Id,
    string CaseNumber,
    ApplicantType ApplicantType,
    CasePhase CurrentPhase,
    CaseStatus CurrentStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? CompletedAt,
    CompanyDto? Company)
    : InvestmentCaseDto(Id, CaseNumber, ApplicantType, CurrentPhase, CurrentStatus, CreatedAt, UpdatedAt, CompletedAt);

public sealed record InvestmentCaseInternalDto(
    Guid Id,
    string CaseNumber,
    string ApplicantUserId,
    ApplicantType ApplicantType,
    CasePhase CurrentPhase,
    CaseStatus CurrentStatus,
    string? WorkflowInstanceId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? CompletedAt,
    CompanyDto? Company)
    : InvestmentCaseDto(Id, CaseNumber, ApplicantType, CurrentPhase, CurrentStatus, CreatedAt, UpdatedAt, CompletedAt);
