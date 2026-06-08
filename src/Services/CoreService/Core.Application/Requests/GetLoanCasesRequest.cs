using BuildingBlocks.Application.Queries;
using Core.Domain.Enums;

namespace Core.Application.Requests;

/// <summary>
/// Advanced filter + pagination request for loan case list endpoints.
/// </summary>
public sealed record GetLoanCasesRequest : PagedListRequest
{
    public string? CaseNumber { get; init; }
    public StringMatchMode CaseNumberMatch { get; init; } = StringMatchMode.Contains;
    public string? ApplicantUserId { get; init; }
    public Guid? CompanyId { get; init; }
    public ApplicantType? ApplicantType { get; init; }

    public string? CompanyName { get; init; }
    public string? FacilitySubject { get; init; }
    public ApplicantCategory? ApplicantCategory { get; init; }

    public LoanCasePhase? CurrentPhase { get; init; }
    public LoanCaseStatus? CurrentStatus { get; init; }

    public LoanFacilityType? FacilityType { get; init; }
    public bool? IsCreditLineActive { get; init; }

    public decimal? RequestedAmountMin { get; init; }
    public decimal? RequestedAmountMax { get; init; }
    public decimal? ApprovedAmountMin { get; init; }
    public decimal? ApprovedAmountMax { get; init; }

    public int? RepaymentMonthsMin { get; init; }
    public int? RepaymentMonthsMax { get; init; }

    /// <summary>When true, returns cases with at least one unpaid installment.</summary>
    public bool? HasUnpaidInstallments { get; init; }

    /// <summary>Gregorian ISO or Jalali (e.g. 1403/01/15).</summary>
    public string? CreatedAtFrom { get; init; }

    /// <summary>Gregorian ISO or Jalali (e.g. 1403/01/15).</summary>
    public string? CreatedAtTo { get; init; }
}
