using BuildingBlocks.Application.Queries;
using BuildingBlocks.Persistence.Queries;
using Core.Application.Requests;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Queries;

public static class LoanCaseQueryFilters
{
    public static IQueryable<LoanCase> ApplyFilters(
        this IQueryable<LoanCase> query,
        GetLoanCasesRequest request,
        DateTimeOffset? createdAtFrom,
        DateTimeOffset? createdAtTo)
    {
        var caseNumber = request.CaseNumber?.Trim();

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
                !string.IsNullOrWhiteSpace(request.FacilitySubject),
                x => x.Application != null
                     && x.Application.FacilitySubject != null
                     && x.Application.FacilitySubject.Contains(request.FacilitySubject!.Trim()))
            .WhereIf(
                request.ApplicantCategory.HasValue,
                x => x.Application != null && x.Application.ApplicantCategory == request.ApplicantCategory)
            .WhereEqualsIf(request.CurrentPhase, x => x.CurrentPhase)
            .WhereEqualsIf(request.CurrentStatus, x => x.CurrentStatus)
            .WhereIf(
                request.FacilityType.HasValue,
                x => x.ApprovalDetail != null && x.ApprovalDetail.FacilityType == request.FacilityType)
            .WhereIf(
                request.IsCreditLineActive.HasValue,
                x => x.ApprovalDetail != null
                     && x.ApprovalDetail.IsCreditLineActive == request.IsCreditLineActive)
            .WhereIf(
                request.RequestedAmountMin.HasValue,
                x => x.Application != null && x.Application.RequestedAmount >= request.RequestedAmountMin)
            .WhereIf(
                request.RequestedAmountMax.HasValue,
                x => x.Application != null && x.Application.RequestedAmount <= request.RequestedAmountMax)
            .WhereIf(
                request.ApprovedAmountMin.HasValue,
                x => x.ApprovalDetail != null
                     && x.ApprovalDetail.ApprovedAmount >= request.ApprovedAmountMin)
            .WhereIf(
                request.ApprovedAmountMax.HasValue,
                x => x.ApprovalDetail != null
                     && x.ApprovalDetail.ApprovedAmount <= request.ApprovedAmountMax)
            .WhereIf(
                request.RepaymentMonthsMin.HasValue,
                x => x.ApprovalDetail != null
                     && x.ApprovalDetail.RepaymentMonths >= request.RepaymentMonthsMin)
            .WhereIf(
                request.RepaymentMonthsMax.HasValue,
                x => x.ApprovalDetail != null
                     && x.ApprovalDetail.RepaymentMonths <= request.RepaymentMonthsMax)
            .WhereIf(
                request.HasUnpaidInstallments == true,
                x => x.Installments.Any(i => !i.IsPaid))
            .WhereIf(
                request.HasUnpaidInstallments == false,
                x => !x.Installments.Any(i => !i.IsPaid))
            .WhereFromIf(createdAtFrom, x => x.CreatedAt)
            .WhereToIf(createdAtTo, x => x.CreatedAt);
    }

    public static IQueryable<LoanCase> ApplySort(
        this IQueryable<LoanCase> query,
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
                ? query.OrderByDescending(x => x.Application!.RequestedAmount)
                : query.OrderBy(x => x.Application!.RequestedAmount),
            "approvedamount" => desc
                ? query.OrderByDescending(x => x.ApprovalDetail!.ApprovedAmount)
                : query.OrderBy(x => x.ApprovalDetail!.ApprovedAmount),
            "repaymentmonths" => desc
                ? query.OrderByDescending(x => x.ApprovalDetail!.RepaymentMonths)
                : query.OrderBy(x => x.ApprovalDetail!.RepaymentMonths),
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
