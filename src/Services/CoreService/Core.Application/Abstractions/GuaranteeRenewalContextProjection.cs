namespace Core.Application.Abstractions;

public sealed record GuaranteeRenewalContextProjection(
    string ApplicantUserId,
    string? ParentBeneficiaryName,
    string? ParentCompanyName,
    string? ApplicantFullName);
