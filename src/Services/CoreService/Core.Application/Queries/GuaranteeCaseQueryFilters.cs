using BuildingBlocks.Application.Queries;
using BuildingBlocks.Persistence.Queries;
using Core.Application.Requests;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Queries;

public static class GuaranteeCaseQueryFilters
{
    public static IQueryable<GuaranteeCase> ApplyFilters(
        this IQueryable<GuaranteeCase> query,
        GetGuaranteeCasesRequest request,
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
            .WhereIf(
                !string.IsNullOrWhiteSpace(request.BeneficiaryName),
                x => x.Application != null
                     && x.Application.BeneficiaryName != null
                     && x.Application.BeneficiaryName.Contains(request.BeneficiaryName!.Trim()))
            .WhereIf(
                !string.IsNullOrWhiteSpace(request.BeneficiaryNationalId),
                x => x.Application != null
                     && x.Application.BeneficiaryNationalId == request.BeneficiaryNationalId!.Trim())
            .WhereIf(
                request.GuaranteeType.HasValue,
                x => x.Application != null && x.Application.GuaranteeType == request.GuaranteeType)
            .WhereEqualsIf(request.CurrentPhase, x => x.CurrentPhase)
            .WhereEqualsIf(request.CurrentStatus, x => x.CurrentStatus)
            .WhereIf(
                request.RequestedGuaranteeAmountMin.HasValue,
                x => x.Application != null
                     && x.Application.RequestedGuaranteeAmount >= request.RequestedGuaranteeAmountMin)
            .WhereIf(
                request.RequestedGuaranteeAmountMax.HasValue,
                x => x.Application != null
                     && x.Application.RequestedGuaranteeAmount <= request.RequestedGuaranteeAmountMax)
            .WhereFromIf(createdAtFrom, x => x.CreatedAt)
            .WhereToIf(createdAtTo, x => x.CreatedAt);
    }

    public static IQueryable<GuaranteeCase> ApplySort(
        this IQueryable<GuaranteeCase> query,
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
            "amount" or "requestedguaranteeamount" => desc
                ? query.OrderByDescending(x => x.Application!.RequestedGuaranteeAmount)
                : query.OrderBy(x => x.Application!.RequestedGuaranteeAmount),
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
