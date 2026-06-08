using System.Text.Json.Serialization;
using Core.Application.Requests;
using Core.Domain.Enums;


namespace Core.Application.DTOs;

[JsonDerivedType(typeof(InvestmentCaseApplicantDto), typeDiscriminator: "applicant")]
[JsonDerivedType(typeof(InvestmentCaseInternalDto), typeDiscriminator: "internal")]
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
    CompanyDto? Company,
    ApplicantContactDto? Applicant = null,
    DataEntry1Dto? ApplicantProfile = null,
    DataEntry2Dto? AttractionBasis = null)
    : InvestmentCaseDto(Id, CaseNumber, ApplicantType, CurrentPhase, CurrentStatus, CreatedAt, UpdatedAt, CompletedAt);

public sealed record InvestmentCaseInternalDto(
    Guid Id,
    string CaseNumber,
    string ApplicantUserId,
    string? ApplicantFullName,
    string? ApplicantPhoneNumber,
    ApplicantType ApplicantType,
    CasePhase CurrentPhase,
    CaseStatus CurrentStatus,
    string? WorkflowInstanceId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? CompletedAt,
    CompanyDto? Company,
    DataEntry1Dto? ApplicantProfile = null,
    DataEntry2Dto? AttractionBasis = null)
    : InvestmentCaseDto(Id, CaseNumber, ApplicantType, CurrentPhase, CurrentStatus, CreatedAt, UpdatedAt, CompletedAt);
