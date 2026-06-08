using BuildingBlocks.Application.Queries;
using BuildingBlocks.Persistence.Queries;
using Core.Application.Requests;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Queries;

public static class InvestmentCaseQueryFilters
{
    public static IQueryable<InvestmentCase> ApplyFilters(
        this IQueryable<InvestmentCase> query,
        GetInvestmentCasesRequest request,
        DateTimeOffset? createdAtFrom,
        DateTimeOffset? createdAtTo)
    {
        var caseNumber = request.CaseNumber?.Trim();
        var contactEmail = request.ContactEmail?.Trim();

        return query
            .WhereIf(
                !string.IsNullOrWhiteSpace(caseNumber) && request.CaseNumberMatch == StringMatchMode.Exact,
                x => x.CaseNumber == caseNumber)
            .WhereContainsIf(
                request.CaseNumberMatch == StringMatchMode.Contains ? caseNumber : null,
                x => x.CaseNumber)
            .WhereIf(request.CompanyId.HasValue, x => x.CompanyId == request.CompanyId)
            .WhereEqualsIf(request.ApplicantType, x => x.ApplicantType)
            .WhereIf(
                !string.IsNullOrWhiteSpace(request.CompanyName),
                x => x.ApplicantCompany != null
                     && x.ApplicantCompany.Name.Contains(request.CompanyName!.Trim()))
            .WhereIf(
                !string.IsNullOrWhiteSpace(request.RepresentativeFullName),
                x => x.ApplicantProfile != null
                     && x.ApplicantProfile.RepresentativeFullName.Contains(request.RepresentativeFullName!.Trim()))
            .WhereIf(
                !string.IsNullOrWhiteSpace(contactEmail) && request.ContactEmailMatch == StringMatchMode.Exact,
                x => x.ApplicantProfile != null && x.ApplicantProfile.ContactEmail == contactEmail)
            .WhereIf(
                !string.IsNullOrWhiteSpace(contactEmail) && request.ContactEmailMatch == StringMatchMode.Contains,
                x => x.ApplicantProfile != null && x.ApplicantProfile.ContactEmail.Contains(contactEmail!))
            .WhereIf(
                request.BusinessStage.HasValue,
                x => x.ApplicantProfile != null && x.ApplicantProfile.BusinessStage == request.BusinessStage)
            .WhereEqualsIf(request.CurrentPhase, x => x.CurrentPhase)
            .WhereEqualsIf(request.CurrentStatus, x => x.CurrentStatus)
            .WhereIf(
                request.RequestedAmountMin.HasValue,
                x => x.ApplicantProfile != null && x.ApplicantProfile.RequestedAmount >= request.RequestedAmountMin)
            .WhereIf(
                request.RequestedAmountMax.HasValue,
                x => x.ApplicantProfile != null && x.ApplicantProfile.RequestedAmount <= request.RequestedAmountMax)
            .WhereIf(
                request.ApprovedAmountMin.HasValue,
                x => x.FinancialWorksheet != null
                     && x.FinancialWorksheet.ApprovedAmount >= request.ApprovedAmountMin)
            .WhereIf(
                request.ApprovedAmountMax.HasValue,
                x => x.FinancialWorksheet != null
                     && x.FinancialWorksheet.ApprovedAmount <= request.ApprovedAmountMax)
            .WhereFromIf(createdAtFrom, x => x.CreatedAt)
            .WhereToIf(createdAtTo, x => x.CreatedAt);
    }

    public static IQueryable<InvestmentCase> ApplySort(
        this IQueryable<InvestmentCase> query,
        string? sortBy,
        SortDirection direction)
    {
        var desc = direction == SortDirection.Desc;
        var key = sortBy?.Trim().ToLowerInvariant();

        return key switch
        {
            "casenumber" => desc
                ? query.OrderByDescending(x => x.CaseNumber)
                : query.OrderBy(x => x.CaseNumber),
            "requestedamount" or "amount" => desc
                ? query.OrderByDescending(x => x.ApplicantProfile!.RequestedAmount)
                : query.OrderBy(x => x.ApplicantProfile!.RequestedAmount),
            "approvedamount" => desc
                ? query.OrderByDescending(x => x.FinancialWorksheet!.ApprovedAmount)
                : query.OrderBy(x => x.FinancialWorksheet!.ApprovedAmount),
            "representativefullname" or "representative" => desc
                ? query.OrderByDescending(x => x.ApplicantProfile!.RepresentativeFullName)
                : query.OrderBy(x => x.ApplicantProfile!.RepresentativeFullName),
            "currentstatus" or "status" => desc
                ? query.OrderByDescending(x => x.CurrentStatus)
                : query.OrderBy(x => x.CurrentStatus),
            "currentphase" or "phase" => desc
                ? query.OrderByDescending(x => x.CurrentPhase)
                : query.OrderBy(x => x.CurrentPhase),
            "updatedat" => desc
                ? query.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                : query.OrderBy(x => x.UpdatedAt ?? x.CreatedAt),
            "createdat" or null or "" => desc
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt),
            _ => desc
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt)
        };
    }
}
