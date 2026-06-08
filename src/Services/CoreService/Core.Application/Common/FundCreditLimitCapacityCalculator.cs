using Core.Application.Abstractions;
using Core.Application.DTOs;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Common;

/// <summary>
/// محاسبه سقف دوره‌ای و مصرف اعتبار صندوق برای ماژول‌های ضمانت‌نامه و تسهیلات.
/// </summary>
public static class FundCreditLimitCapacityCalculator
{
    private static readonly GuaranteeCaseStatus[] GuaranteeActiveCommitmentStatuses =
    [
        GuaranteeCaseStatus.CreditReview,
        GuaranteeCaseStatus.ApprovalFormEntry,
        GuaranteeCaseStatus.CeoApprovalInitial,
        GuaranteeCaseStatus.WaitingDraftContract,
        GuaranteeCaseStatus.WaitingSignedContractAndAttachments,
        GuaranteeCaseStatus.FinancialAttachmentReview,
        GuaranteeCaseStatus.WaitingFinalContract,
        GuaranteeCaseStatus.CeoApprovalFinal,
        GuaranteeCaseStatus.WaitingIssuanceDocuments,
    ];

    private static readonly LoanCaseStatus[] LoanActivePipelineStatuses =
    [
        LoanCaseStatus.PendingCeoInitialApproval,
        LoanCaseStatus.PendingLegalRawContract,
        LoanCaseStatus.PendingApplicantSignature,
        LoanCaseStatus.PendingLegalFinalReview,
        LoanCaseStatus.RevisionRequestedByLegal,
        LoanCaseStatus.PendingFinancialReview,
        LoanCaseStatus.RevisionRequestedByFinancial,
        LoanCaseStatus.PendingLegalFinalContract,
        LoanCaseStatus.PendingCeoFinalApproval,
        LoanCaseStatus.ReadyForPayment,
    ];

    private static readonly LoanCaseStatus[] LoanDisbursedStatuses =
    [
        LoanCaseStatus.RepaymentPhase,
        LoanCaseStatus.Completed,
        LoanCaseStatus.Archived,
    ];

    public static async Task<FundCreditCapacitySnapshotDto> ComputeActiveAsync(
        ICoreDbContext db,
        FundModuleType moduleType,
        DateOnly referenceDate,
        CancellationToken cancellationToken)
    {
        var pool = await ResolveActivePoolAsync(db, moduleType, referenceDate, cancellationToken);
        if (pool is null)
            return new FundCreditCapacitySnapshotDto(moduleType, null, null, null, null, null);

        var utilization = await ComputeUtilizationAsync(db, moduleType, pool.PeriodStart, pool.ExpiresAt, cancellationToken);
        return new FundCreditCapacitySnapshotDto(
            moduleType,
            pool.CreditLimitWithCheck,
            utilization,
            pool.CreditLimitWithCheck - utilization,
            pool.PeriodStart,
            pool.ExpiresAt);
    }

    public static async Task<FundCreditCapacitySnapshotDto> ComputeForPoolAsync(
        ICoreDbContext db,
        Guid poolId,
        CancellationToken cancellationToken)
    {
        var pool = await db.FundCreditLimits
            .AsNoTracking()
            .Where(x => x.Id == poolId)
            .Select(x => new PoolProjection(x.Id, x.ModuleType, x.CreditLimitWithCheck, x.PeriodStart, x.ExpiresAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (pool is null)
            return new FundCreditCapacitySnapshotDto(FundModuleType.Guarantee, null, null, null, null, null);

        var utilization = await ComputeUtilizationAsync(db, pool.ModuleType, pool.PeriodStart, pool.ExpiresAt, cancellationToken);
        return new FundCreditCapacitySnapshotDto(
            pool.ModuleType,
            pool.CreditLimitWithCheck,
            utilization,
            pool.CreditLimitWithCheck - utilization,
            pool.PeriodStart,
            pool.ExpiresAt);
    }

    public static async Task<PoolProjection?> ResolveActivePoolAsync(
        ICoreDbContext db,
        FundModuleType moduleType,
        DateOnly referenceDate,
        CancellationToken cancellationToken)
        => await db.FundCreditLimits
            .AsNoTracking()
            .Where(x => x.ModuleType == moduleType
                && x.PeriodStart <= referenceDate
                && x.ExpiresAt >= referenceDate)
            .OrderByDescending(x => x.PeriodStart)
            .Select(x => new PoolProjection(x.Id, x.ModuleType, x.CreditLimitWithCheck, x.PeriodStart, x.ExpiresAt))
            .FirstOrDefaultAsync(cancellationToken);

    public static async Task<decimal> ComputeUtilizationAsync(
        ICoreDbContext db,
        FundModuleType moduleType,
        DateOnly periodStart,
        DateOnly expiresAt,
        CancellationToken cancellationToken)
        => moduleType switch
        {
            FundModuleType.Guarantee => await ComputeGuaranteeUtilizationAsync(db, periodStart, expiresAt, cancellationToken),
            FundModuleType.Loan => await ComputeLoanUtilizationAsync(db, periodStart, expiresAt, cancellationToken),
            _ => 0m
        };

    public static async Task<bool> HasOverlappingPeriodAsync(
        ICoreDbContext db,
        FundModuleType moduleType,
        DateOnly periodStart,
        DateOnly expiresAt,
        Guid? excludeId,
        CancellationToken cancellationToken)
        => await db.FundCreditLimits
            .AsNoTracking()
            .AnyAsync(
                x => x.ModuleType == moduleType
                    && x.Id != excludeId
                    && periodStart <= x.ExpiresAt
                    && expiresAt >= x.PeriodStart,
                cancellationToken);

    private static async Task<decimal> ComputeGuaranteeUtilizationAsync(
        ICoreDbContext db,
        DateOnly periodStart,
        DateOnly expiresAt,
        CancellationToken cancellationToken)
    {
        var cases = await db.GuaranteeCases
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new GuaranteeCreditProjection(
                c.CurrentStatus,
                c.CreatedAt,
                c.CompletedAt,
                c.ApprovalForm != null
                    ? c.ApprovalForm.GuaranteeAmount
                    : c.Application != null
                        ? c.Application.RequestedGuaranteeAmount
                        : null,
                c.ApprovalForm != null ? c.ApprovalForm.IssuanceDate : null))
            .ToListAsync(cancellationToken);

        var issued = cases
            .Where(c => c.Status == GuaranteeCaseStatus.Completed && c.Amount is > 0)
            .Where(c => IsReferenceDateInPeriod(GetGuaranteeIssuedReferenceDate(c), periodStart, expiresAt))
            .Sum(c => c.Amount!.Value);

        var active = cases
            .Where(c => GuaranteeActiveCommitmentStatuses.Contains(c.Status) && c.Amount is > 0)
            .Where(c => IsCaseCreatedInPeriod(c.CreatedAt, periodStart, expiresAt))
            .Sum(c => c.Amount!.Value);

        return issued + active;
    }

    private static async Task<decimal> ComputeLoanUtilizationAsync(
        ICoreDbContext db,
        DateOnly periodStart,
        DateOnly expiresAt,
        CancellationToken cancellationToken)
    {
        var cases = await db.LoanCases
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new LoanCreditProjection(
                c.CurrentStatus,
                c.CreatedAt,
                c.CompletedAt,
                c.ApprovalDetail != null
                    ? c.ApprovalDetail.ApprovedAmount
                    : c.Application != null
                        ? c.Application.RequestedAmount
                        : null))
            .ToListAsync(cancellationToken);

        var disbursed = cases
            .Where(c => LoanDisbursedStatuses.Contains(c.Status) && c.Amount is > 0)
            .Where(c => IsReferenceDateInPeriod(GetLoanDisbursedReferenceDate(c), periodStart, expiresAt))
            .Sum(c => c.Amount!.Value);

        var active = cases
            .Where(c => LoanActivePipelineStatuses.Contains(c.Status) && c.Amount is > 0)
            .Where(c => IsCaseCreatedInPeriod(c.CreatedAt, periodStart, expiresAt))
            .Sum(c => c.Amount!.Value);

        return disbursed + active;
    }

    private static bool IsReferenceDateInPeriod(DateOnly? date, DateOnly periodStart, DateOnly expiresAt)
        => date is not null && date.Value >= periodStart && date.Value <= expiresAt;

    private static bool IsCaseCreatedInPeriod(DateTimeOffset createdAt, DateOnly periodStart, DateOnly expiresAt)
    {
        var created = DateOnly.FromDateTime(createdAt.UtcDateTime);
        return created >= periodStart && created <= expiresAt;
    }

    private static DateOnly? GetGuaranteeIssuedReferenceDate(GuaranteeCreditProjection c)
    {
        if (c.IssuanceDate is not null)
            return c.IssuanceDate;

        if (c.CompletedAt is not null)
            return DateOnly.FromDateTime(c.CompletedAt.Value.UtcDateTime);

        return null;
    }

    private static DateOnly? GetLoanDisbursedReferenceDate(LoanCreditProjection c)
    {
        if (c.CompletedAt is not null)
            return DateOnly.FromDateTime(c.CompletedAt.Value.UtcDateTime);

        return null;
    }

    public sealed record PoolProjection(
        Guid Id,
        FundModuleType ModuleType,
        decimal CreditLimitWithCheck,
        DateOnly PeriodStart,
        DateOnly ExpiresAt);

    private sealed record GuaranteeCreditProjection(
        GuaranteeCaseStatus Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt,
        decimal? Amount,
        DateOnly? IssuanceDate);

    private sealed record LoanCreditProjection(
        LoanCaseStatus Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt,
        decimal? Amount);
}
