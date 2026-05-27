using Core.Application.Abstractions;
using Core.Application.DTOs;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Common;

/// <summary>
/// جدول ۱ فرم تصویب — سقف فعال صندوق و جمع صادره/تعهدات فقط در بازه سقف جاری (از شروع سقف تا تاریخ انقضا).
/// </summary>
public static class GuaranteeApplicantCreditSnapshotCalculator
{
    private static readonly GuaranteeCaseStatus[] ActiveCommitmentStatuses =
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

    public static Task<GuaranteeApplicantCreditSnapshotDto> ComputeAsync(
        ICoreDbContext db,
        GuaranteeCase current,
        CancellationToken cancellationToken)
        => ComputeFundSnapshotAsync(db, current, cancellationToken);

    public static async Task<GuaranteeApplicantCreditSnapshotDto> ComputeFundSnapshotAsync(
        ICoreDbContext db,
        GuaranteeCase? currentCase,
        CancellationToken cancellationToken)
    {
        var settings = await ResolveFundCreditLimitSettingsAsync(db, cancellationToken);
        if (settings is null || settings.CreditLimitWithCheck <= 0)
        {
            return new GuaranteeApplicantCreditSnapshotDto(null, null, null, null, null, null);
        }

        var periodStart = settings.PeriodStart;
        var expiresAt = settings.ExpiresAt;

        var cases = await db.GuaranteeCases
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new CaseCreditProjection(
                c.Id,
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

        var fundIssued = cases
            .Where(c => c.Status == GuaranteeCaseStatus.Completed && c.Amount is > 0)
            .Where(c => IsReferenceDateInPeriod(GetIssuedReferenceDate(c), periodStart, expiresAt))
            .Sum(c => c.Amount!.Value);

        var activeCommitments = cases
            .Where(c => ActiveCommitmentStatuses.Contains(c.Status) && c.Amount is > 0)
            .Where(c => IsCaseInActivePeriod(c, periodStart, expiresAt))
            .Sum(c => c.Amount!.Value);

        var creditLimit = settings.CreditLimitWithCheck;

        decimal? remaining = creditLimit - fundIssued - activeCommitments;

        return new GuaranteeApplicantCreditSnapshotDto(
            creditLimit,
            fundIssued,
            activeCommitments,
            remaining,
            periodStart,
            expiresAt);
    }

    public static async Task<FundCreditLimitSettings?> ResolveFundCreditLimitSettingsAsync(
        ICoreDbContext db,
        CancellationToken cancellationToken)
    {
        return await db.GuaranteeFundCreditLimits
            .AsNoTracking()
            .Where(x => x.Id == GuaranteeFundCreditLimit.SingletonId)
            .Select(x => new FundCreditLimitSettings(
                x.CreditLimitWithCheck,
                x.PeriodStart,
                x.ExpiresAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<decimal> ResolveFundCreditLimitAsync(
        ICoreDbContext db,
        CancellationToken cancellationToken)
    {
        var settings = await ResolveFundCreditLimitSettingsAsync(db, cancellationToken);
        return settings?.CreditLimitWithCheck ?? 0m;
    }

    public static decimal ResolveCaseAmount(GuaranteeCase guaranteeCase)
    {
        var amount = guaranteeCase.ApprovalForm?.GuaranteeAmount
            ?? guaranteeCase.Application?.RequestedGuaranteeAmount;
        return amount is > 0 ? amount.Value : 0m;
    }

    private static bool IsReferenceDateInPeriod(DateOnly? date, DateOnly periodStart, DateOnly expiresAt)
        => date is not null && date.Value >= periodStart && date.Value <= expiresAt;

    private static bool IsCaseInActivePeriod(CaseCreditProjection c, DateOnly periodStart, DateOnly expiresAt)
    {
        var created = DateOnly.FromDateTime(c.CreatedAt.UtcDateTime);
        return created >= periodStart && created <= expiresAt;
    }

    private static DateOnly? GetIssuedReferenceDate(CaseCreditProjection c)
    {
        if (c.IssuanceDate is not null)
            return c.IssuanceDate;

        if (c.CompletedAt is not null)
            return DateOnly.FromDateTime(c.CompletedAt.Value.UtcDateTime);

        return null;
    }

    private sealed record CaseCreditProjection(
        Guid Id,
        GuaranteeCaseStatus Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt,
        decimal? Amount,
        DateOnly? IssuanceDate);

    public sealed record FundCreditLimitSettings(
        decimal CreditLimitWithCheck,
        DateOnly PeriodStart,
        DateOnly ExpiresAt);
}
