using BuildingBlocks.Application.Queries;
using Core.Domain.Enums;

namespace Core.Application.Requests;

/// <summary>
/// Advanced filter + pagination request for guarantee case list endpoints.
/// </summary>
public sealed record GetGuaranteeCasesRequest : PagedListRequest
{
    public string? CaseNumber { get; init; }
    public StringMatchMode CaseNumberMatch { get; init; } = StringMatchMode.Contains;
    public string? ApplicantUserId { get; init; }
    public Guid? CompanyId { get; init; }

    public string? BeneficiaryName { get; init; }
    public string? BeneficiaryNationalId { get; init; }

    public GuaranteeType? GuaranteeType { get; init; }
    public GuaranteeCasePhase? CurrentPhase { get; init; }
    public GuaranteeCaseStatus? CurrentStatus { get; init; }

    public decimal? RequestedGuaranteeAmountMin { get; init; }
    public decimal? RequestedGuaranteeAmountMax { get; init; }

    /// <summary>Gregorian ISO or Jalali (e.g. 1403/01/15).</summary>
    public string? CreatedAtFrom { get; init; }

    /// <summary>Gregorian ISO or Jalali (e.g. 1403/01/15).</summary>
    public string? CreatedAtTo { get; init; }
}
