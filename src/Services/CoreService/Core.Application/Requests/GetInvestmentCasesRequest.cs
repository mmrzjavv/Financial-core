using BuildingBlocks.Application.Queries;
using Core.Domain.Enums;

namespace Core.Application.Requests;

/// <summary>
/// Advanced filter + pagination request for investment case list endpoints.
/// </summary>
public sealed record GetInvestmentCasesRequest : PagedListRequest
{
    public string? CaseNumber { get; init; }
    public StringMatchMode CaseNumberMatch { get; init; } = StringMatchMode.Contains;
    public string? ApplicantUserId { get; init; }
    public Guid? CompanyId { get; init; }
    public ApplicantType? ApplicantType { get; init; }

    public string? CompanyName { get; init; }
    public string? RepresentativeFullName { get; init; }
    public string? ContactEmail { get; init; }
    public StringMatchMode ContactEmailMatch { get; init; } = StringMatchMode.Contains;
    public BusinessStage? BusinessStage { get; init; }

    public CasePhase? CurrentPhase { get; init; }
    public CaseStatus? CurrentStatus { get; init; }

    public decimal? RequestedAmountMin { get; init; }
    public decimal? RequestedAmountMax { get; init; }
    public decimal? ApprovedAmountMin { get; init; }
    public decimal? ApprovedAmountMax { get; init; }

    /// <summary>Gregorian ISO or Jalali (e.g. 1403/01/15).</summary>
    public string? CreatedAtFrom { get; init; }

    /// <summary>Gregorian ISO or Jalali (e.g. 1403/01/15).</summary>
    public string? CreatedAtTo { get; init; }
}
