using System.Text.Json;
using Core.Application.Abstractions;
using Core.Application.Kanban;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Dashboard;

public sealed class DashboardAggregationService(
    ICoreDbContext dbContext,
    IDashboardStatsRepository statsRepository,
    ILogger<DashboardAggregationService> logger) : IDashboardAggregationService
{
    private sealed record CaseStatusProjection(int Status);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly int[] InvestmentTerminal =
    [
        (int)CaseStatus.Completed, (int)CaseStatus.Rejected, (int)CaseStatus.Cancelled, (int)CaseStatus.Archived
    ];

    private static readonly int[] GuaranteeTerminal =
    [
        (int)GuaranteeCaseStatus.Completed, (int)GuaranteeCaseStatus.Rejected,
        (int)GuaranteeCaseStatus.Cancelled, (int)GuaranteeCaseStatus.Archived
    ];

    private static readonly int[] LoanTerminal =
    [
        (int)LoanCaseStatus.Completed, (int)LoanCaseStatus.Archived, (int)LoanCaseStatus.CanceledByCeo
    ];

    public async Task AggregateAllAsync(CancellationToken cancellationToken = default)
    {
        var computedAt = DateTimeOffset.UtcNow;
        logger.LogInformation("Dashboard aggregation started at {ComputedAt}", computedAt);

        await UpsertExecutiveAsync(computedAt, cancellationToken);

        foreach (var departmentKey in DashboardRoleResolver.DepartmentKeys)
            await UpsertDepartmentAsync(departmentKey, computedAt, cancellationToken);

        await UpsertApplicantSnapshotsAsync(computedAt, cancellationToken);

        logger.LogInformation("Dashboard aggregation completed at {ComputedAt}", computedAt);
    }

    private async Task UpsertExecutiveAsync(DateTimeOffset computedAt, CancellationToken ct)
    {
        var monthStart = new DateTimeOffset(computedAt.Year, computedAt.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var sixMonthsAgo = computedAt.AddMonths(-6);
        var onlineThreshold = computedAt.AddMinutes(-30);
        var dayStart = computedAt.UtcDateTime.Date;

        // #region agent log
        try
        {
            var logLine = JsonSerializer.Serialize(new
            {
                sessionId = "757b3c",
                runId = "pre-fix",
                hypothesisId = "A",
                location = "DashboardAggregationService.cs:UpsertExecutiveAsync",
                message = "UserSessions query DateTime parameter kinds",
                data = new
                {
                    dayStartKind = dayStart.Kind.ToString(),
                    onlineThresholdKind = onlineThreshold.UtcDateTime.Kind.ToString(),
                    computedAtDateKind = computedAt.Date.Kind.ToString()
                },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }) + "\n";
            await File.AppendAllTextAsync(@"D:\work\Maskan\Panel\Financial-Core\debug-757b3c.log", logLine, ct);
        }
        catch { /* ignore debug log failures */ }
        // #endregion

        var investmentCases = await dbContext.InvestmentCases.AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new InvestmentCaseProjection(
                c.Id, c.CaseNumber, (int)c.CurrentStatus, (int)c.CurrentPhase, c.CreatedAt, c.UpdatedAt))
            .ToListAsync(ct);

        var guaranteeCases = await dbContext.GuaranteeCases.AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new CaseStatusProjection((int)c.CurrentStatus))
            .ToListAsync(ct);

        var loanCases = await dbContext.LoanCases.AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new CaseStatusProjection((int)c.CurrentStatus))
            .ToListAsync(ct);

        var activeInvestmentVolume = await dbContext.InvestmentCaseApplicantProfiles.AsNoTracking()
            .Where(d => !d.Case.IsDeleted && !InvestmentTerminal.Contains((int)d.Case.CurrentStatus))
            .SumAsync(d => (decimal?)d.RequestedAmount, ct) ?? 0m;

        var activeGuaranteeVolume = await dbContext.GuaranteeCaseApplications.AsNoTracking()
            .Where(a => !a.Case.IsDeleted && !GuaranteeTerminal.Contains((int)a.Case.CurrentStatus))
            .SumAsync(a => (decimal?)a.RequestedGuaranteeAmount, ct) ?? 0m;

        var activeLoanVolume = await dbContext.LoanCaseApplications.AsNoTracking()
            .Where(a => !a.Case.IsDeleted && !LoanTerminal.Contains((int)a.Case.CurrentStatus))
            .SumAsync(a => (decimal?)a.RequestedAmount, ct) ?? 0m;

        var totalRequested = await dbContext.InvestmentCaseApplicantProfiles.AsNoTracking()
            .Where(d => !d.Case.IsDeleted)
            .SumAsync(d => (decimal?)d.RequestedAmount, ct) ?? 0m;

        var approvedPayments = await dbContext.PaymentRecords.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Completed && !p.Case.IsDeleted)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        var loanPayments = await dbContext.LoanPayments.AsNoTracking()
            .Where(p => !p.Case.IsDeleted)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        var pendingCount = CountPendingCases(investmentCases, guaranteeCases, loanCases);
        var completedCount = CountCompletedCases(investmentCases, guaranteeCases, loanCases);
        var canceledCount = CountCanceledCases(investmentCases, guaranteeCases, loanCases);
        var totalCases = investmentCases.Count + guaranteeCases.Count + loanCases.Count;
        var activeCount = pendingCount;
        var completionRate = totalCases == 0 ? 0 : Math.Round(completedCount * 100.0 / totalCases, 1);

        var statusDistribution = new List<StatusBucketDto>
        {
            new("Pending", "در جریان", pendingCount),
            new("Completed", "تکمیل‌شده", completedCount),
            new("Canceled", "لغو/رد", canceledCount)
        };

        var monthlyFinancial = await BuildMonthlyFinancialOutputAsync(sixMonthsAgo, ct);

        var onlineUsers = await dbContext.UserSessions.AsNoTracking()
            .CountAsync(s => s.RevokedAt == null && s.LastActivityAt >= onlineThreshold.UtcDateTime, ct);

        var dailyActiveUsers = await dbContext.UserSessions.AsNoTracking()
            .Where(s => s.LastActivityAt >= dayStart)
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync(ct);

        var bottlenecks = await BuildDepartmentBottlenecksAsync(ct);

        var pipelineByStatus = investmentCases
            .GroupBy(c => c.Status)
            .Select(g => new StatusCountDto(g.Key, CaseKanbanRules.GetStatusTitle((CaseStatus)g.Key), g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var countsByPhase = investmentCases
            .GroupBy(c => c.Phase)
            .Select(g => new StatusCountDto(g.Key, CaseKanbanRules.GetPhaseTitle((CasePhase)g.Key), g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var monthlyTrend = investmentCases
            .Where(c => c.CreatedAt >= sixMonthsAgo)
            .GroupBy(c => new { Year = c.CreatedAt.Year, Month = c.CreatedAt.Month })
            .Select(g => new MonthlyCountDto(g.Key.Year, g.Key.Month, g.Count()))
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        var recentActivity = await dbContext.CaseWorkflowHistories.AsNoTracking()
            .OrderByDescending(h => h.CreatedAt)
            .Take(15)
            .Select(h => new RecentActivityDto(
                h.CaseId, h.Case.CaseNumber, (int)h.FromStatus, (int)h.ToStatus, h.Action, h.CreatedAt))
            .ToListAsync(ct);

        var pendingCeo = investmentCases.Count(c => c.Status == (int)CaseStatus.WaitingCeoApproval)
            + guaranteeCases.Count(c => c.Status is (int)GuaranteeCaseStatus.CeoApprovalInitial or (int)GuaranteeCaseStatus.CeoApprovalFinal)
            + loanCases.Count(c => c.Status is (int)LoanCaseStatus.PendingCeoInitialApproval or (int)LoanCaseStatus.PendingCeoFinalApproval);

        var waitingPayment = investmentCases.Count(c => c.Status == (int)CaseStatus.WaitingPayment);
        var rejectedCount = investmentCases.Count(c => c.Status == (int)CaseStatus.Rejected)
            + guaranteeCases.Count(c => c.Status == (int)GuaranteeCaseStatus.Rejected)
            + loanCases.Count(c => c.Status == (int)LoanCaseStatus.CanceledByCeo);

        var casesThisMonth = investmentCases.Count(c => c.CreatedAt >= monthStart);

        var avgReviewDays = await ComputeAverageReviewDaysAsync(ct);

        var activePipelineRequested = await dbContext.InvestmentCaseApplicantProfiles.AsNoTracking()
            .Where(d => !d.Case.IsDeleted && !InvestmentTerminal.Contains((int)d.Case.CurrentStatus))
            .SumAsync(d => (decimal?)d.RequestedAmount, ct) ?? 0m;

        var payload = new ExecutiveStatsCachePayload
        {
            Data = new ExecutiveDashboardViewDto
            {
                ActiveGuaranteesVolume = activeGuaranteeVolume,
                ActiveInvestmentsVolume = activeInvestmentVolume,
                ActiveLoansVolume = activeLoanVolume,
                StatusDistribution = statusDistribution,
                MonthlyFinancialOutput = monthlyFinancial,
                OnlineUsersCount = onlineUsers,
                DailyActiveUsers = dailyActiveUsers,
                DepartmentBottlenecks = bottlenecks,
                PipelineByStatus = pipelineByStatus,
                RecentActivity = recentActivity,
                TotalActiveCases = activeCount,
                CompletedCases = completedCount,
                PendingCeoApprovals = pendingCeo,
                WaitingPaymentCount = waitingPayment,
                RejectedCount = rejectedCount,
                CompletionRate = completionRate,
                TotalRequestedAmount = totalRequested,
                ApprovedPaymentsSum = approvedPayments + loanPayments,
                CasesThisMonth = casesThisMonth,
                AverageDaysInReview = avgReviewDays,
                ActivePipelineRequestedAmount = activePipelineRequested,
                CountsByPhase = countsByPhase,
                MonthlyTrend = monthlyTrend,
                TotalCases = totalCases
            }
        };

        await statsRepository.UpsertAsync(new DashboardStatsSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotKey = DashboardRoleResolver.ExecutiveSnapshotKey,
            SnapshotType = DashboardSnapshotType.Executive,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            ComputedAtUtc = computedAt
        }, ct);
    }

    private async Task UpsertDepartmentAsync(string departmentKey, DateTimeOffset computedAt, CancellationToken ct)
    {
        var repRole = DashboardRoleResolver.GetDepartmentRepresentativeRole(departmentKey);
        var title = DashboardRoleResolver.GetDepartmentTitle(departmentKey);

        var investmentQueue = await dbContext.InvestmentCases.AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new { c.CurrentStatus })
            .ToListAsync(ct);

        var guaranteeQueue = await dbContext.GuaranteeCases.AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new { c.CurrentStatus })
            .ToListAsync(ct);

        var loanQueue = await dbContext.LoanCases.AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new { c.CurrentStatus })
            .ToListAsync(ct);

        var renewalQueue = await dbContext.GuaranteeRenewalCases.AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new { c.CurrentStatus })
            .ToListAsync(ct);

        var investmentCount = investmentQueue.Count(c => CaseKanbanRules.IsActionRequired(c.CurrentStatus, repRole));
        var guaranteeCount = guaranteeQueue.Count(c => GuaranteeKanbanRules.IsActionRequired(c.CurrentStatus, repRole));
        var loanCount = loanQueue.Count(c => LoanKanbanRules.IsActionRequired(c.CurrentStatus, repRole));
        var renewalCount = renewalQueue.Count(c =>
            c.CurrentStatus == GuaranteeRenewalStatus.CeoReview && repRole == UserRoleClaims.Ceo);

        var queueByModule = new List<ModuleQueueCountDto>
        {
            new("Investment", "سرمایه‌گذاری", investmentCount),
            new("Guarantee", "ضمانت‌نامه", guaranteeCount),
            new("Loan", "تسهیلات", loanCount),
            new("GuaranteeRenewal", "تمدید ضمانت", renewalCount)
        };

        var totalQueue = queueByModule.Sum(x => x.Count);
        var revisionRate = await ComputeRevisionRateAsync(departmentKey, totalQueue, ct);

        var payload = new DepartmentStatsCachePayload
        {
            DepartmentKey = departmentKey,
            DepartmentTitle = title,
            TotalQueueCount = totalQueue,
            QueueByModule = queueByModule,
            RevisionRatePercent = revisionRate
        };

        await statsRepository.UpsertAsync(new DashboardStatsSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotKey = DashboardRoleResolver.DepartmentSnapshotKey(departmentKey),
            SnapshotType = DashboardSnapshotType.Department,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            ComputedAtUtc = computedAt
        }, ct);
    }

    private async Task UpsertApplicantSnapshotsAsync(DateTimeOffset computedAt, CancellationToken ct)
    {
        var applicantIds = await dbContext.InvestmentCases.AsNoTracking()
            .Where(c => !c.IsDeleted && !InvestmentTerminal.Contains((int)c.CurrentStatus))
            .Select(c => c.ApplicantUserId)
            .Union(dbContext.GuaranteeCases.AsNoTracking()
                .Where(c => !c.IsDeleted && !GuaranteeTerminal.Contains((int)c.CurrentStatus))
                .Select(c => c.ApplicantUserId))
            .Union(dbContext.LoanCases.AsNoTracking()
                .Where(c => !c.IsDeleted && !LoanTerminal.Contains((int)c.CurrentStatus))
                .Select(c => c.ApplicantUserId))
            .ToListAsync(ct);

        foreach (var applicantId in applicantIds)
            await UpsertApplicantAsync(applicantId, computedAt, ct);
    }

    private async Task UpsertApplicantAsync(string applicantUserId, DateTimeOffset computedAt, CancellationToken ct)
    {
        var userId = applicantUserId;
        var activeCases = new List<ApplicantCaseProgressDto>();

        var investments = await dbContext.InvestmentCases.AsNoTracking()
            .Where(c => !c.IsDeleted && c.ApplicantUserId == applicantUserId && !InvestmentTerminal.Contains((int)c.CurrentStatus))
            .Select(c => new { c.Id, c.CaseNumber, c.CurrentStatus, c.CurrentPhase, c.UpdatedAt, c.CreatedAt })
            .ToListAsync(ct);

        foreach (var c in investments)
        {
            activeCases.Add(new ApplicantCaseProgressDto(
                c.Id, c.CaseNumber, "Investment", "سرمایه‌گذاری",
                (int)c.CurrentStatus, CaseKanbanRules.GetStatusTitle(c.CurrentStatus),
                CaseKanbanRules.GetPhaseTitle(c.CurrentPhase),
                ComputeProgressPercent((int)c.CurrentStatus, InvestmentTerminal, (int)CaseStatus.Completed),
                c.UpdatedAt ?? c.CreatedAt));
        }

        var guarantees = await dbContext.GuaranteeCases.AsNoTracking()
            .Where(c => !c.IsDeleted && c.ApplicantUserId == applicantUserId && !GuaranteeTerminal.Contains((int)c.CurrentStatus))
            .Select(c => new { c.Id, c.CaseNumber, c.CurrentStatus, c.CurrentPhase, c.UpdatedAt, c.CreatedAt })
            .ToListAsync(ct);

        foreach (var c in guarantees)
        {
            activeCases.Add(new ApplicantCaseProgressDto(
                c.Id, c.CaseNumber, "Guarantee", "ضمانت‌نامه",
                (int)c.CurrentStatus, GuaranteeKanbanRules.GetStatusTitle(c.CurrentStatus),
                GuaranteeKanbanRules.GetPhaseTitle(c.CurrentPhase),
                ComputeProgressPercent((int)c.CurrentStatus, GuaranteeTerminal, (int)GuaranteeCaseStatus.Completed),
                c.UpdatedAt ?? c.CreatedAt));
        }

        var loans = await dbContext.LoanCases.AsNoTracking()
            .Where(c => !c.IsDeleted && c.ApplicantUserId == applicantUserId && !LoanTerminal.Contains((int)c.CurrentStatus))
            .Select(c => new { c.Id, c.CaseNumber, c.CurrentStatus, c.CurrentPhase, c.UpdatedAt, c.CreatedAt })
            .ToListAsync(ct);

        foreach (var c in loans)
        {
            activeCases.Add(new ApplicantCaseProgressDto(
                c.Id, c.CaseNumber, "Loan", "تسهیلات",
                (int)c.CurrentStatus, LoanKanbanRules.GetStatusTitle(c.CurrentStatus),
                LoanKanbanRules.GetPhaseTitle(c.CurrentPhase),
                ComputeProgressPercent((int)c.CurrentStatus, LoanTerminal, (int)LoanCaseStatus.Completed),
                c.UpdatedAt ?? c.CreatedAt));
        }

        var unpaidInstallments = await dbContext.LoanInstallments.AsNoTracking()
            .Where(i => !i.IsPaid && !i.Case.IsDeleted && i.Case.ApplicantUserId == applicantUserId)
            .Select(i => new { i.TotalAmount })
            .ToListAsync(ct);

        var recentComments = new List<ApplicantRecentCommentDto>();

        var investmentComments = await dbContext.CaseComments.AsNoTracking()
            .Where(c => c.Case.ApplicantUserId == applicantUserId && !c.Case.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .Select(c => new ApplicantRecentCommentDto(
                c.CaseId, c.Case.CaseNumber, "Investment", "سرمایه‌گذاری",
                c.Message, c.SenderRole ?? "", c.CreatedAt))
            .ToListAsync(ct);
        recentComments.AddRange(investmentComments);

        var guaranteeComments = await dbContext.GuaranteeCaseComments.AsNoTracking()
            .Where(c => c.Case.ApplicantUserId == applicantUserId && !c.Case.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .Select(c => new ApplicantRecentCommentDto(
                c.CaseId, c.Case.CaseNumber, "Guarantee", "ضمانت‌نامه",
                c.Message, c.SenderRole ?? "", c.CreatedAt))
            .ToListAsync(ct);
        recentComments.AddRange(guaranteeComments);

        var loanComments = await dbContext.LoanCaseComments.AsNoTracking()
            .Where(c => c.Case.ApplicantUserId == applicantUserId && !c.Case.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .Select(c => new ApplicantRecentCommentDto(
                c.CaseId, c.Case.CaseNumber, "Loan", "تسهیلات",
                c.Message, c.SenderRole ?? "", c.CreatedAt))
            .ToListAsync(ct);
        recentComments.AddRange(loanComments);

        recentComments = recentComments.OrderByDescending(c => c.CreatedAt).Take(10).ToList();

        var payload = new ApplicantStatsCachePayload
        {
            UserId = userId,
            ActiveCases = activeCases.OrderByDescending(c => c.UpdatedAt).ToList(),
            TotalRemainingDebt = unpaidInstallments.Sum(i => i.TotalAmount),
            UnpaidInstallmentsCount = unpaidInstallments.Count,
            RecentComments = recentComments
        };

        await statsRepository.UpsertAsync(new DashboardStatsSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotKey = DashboardRoleResolver.ApplicantSnapshotKey(userId),
            SnapshotType = DashboardSnapshotType.Applicant,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            ComputedAtUtc = computedAt
        }, ct);
    }

    private async Task<IReadOnlyList<MonthlyFinancialOutputDto>> BuildMonthlyFinancialOutputAsync(
        DateTimeOffset since,
        CancellationToken ct)
    {
        var investmentPayments = await dbContext.PaymentRecords.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Completed && !p.Case.IsDeleted && p.CreatedAt >= since)
            .Select(p => new { p.CreatedAt, p.Amount })
            .ToListAsync(ct);

        var loanPayments = await dbContext.LoanPayments.AsNoTracking()
            .Where(p => !p.Case.IsDeleted && p.CreatedAt >= since)
            .Select(p => new { p.CreatedAt, p.Amount })
            .ToListAsync(ct);

        var grouped = investmentPayments
            .Select(p => new { p.CreatedAt.Year, p.CreatedAt.Month, p.Amount })
            .Concat(loanPayments.Select(p => new { p.CreatedAt.Year, p.CreatedAt.Month, p.Amount }))
            .GroupBy(x => new { x.Year, x.Month })
            .Select(g => new MonthlyFinancialOutputDto(
                g.Key.Year, g.Key.Month, g.Sum(x => x.Amount), g.Count()))
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        return grouped;
    }

    private async Task<IReadOnlyList<DepartmentBottleneckDto>> BuildDepartmentBottlenecksAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var result = new List<DepartmentBottleneckDto>();

        var legalInvestment = await dbContext.InvestmentCases.AsNoTracking()
            .Where(c => !c.IsDeleted && c.CurrentPhase == CasePhase.Legal && c.UpdatedAt != null)
            .Select(c => (now - c.UpdatedAt!.Value).TotalDays)
            .ToListAsync(ct);

        var legalGuarantee = await dbContext.GuaranteeCases.AsNoTracking()
            .Where(c => !c.IsDeleted && c.CurrentPhase == GuaranteeCasePhase.Legal && c.UpdatedAt != null)
            .Select(c => (now - c.UpdatedAt!.Value).TotalDays)
            .ToListAsync(ct);

        var legalLoan = await dbContext.LoanCases.AsNoTracking()
            .Where(c => !c.IsDeleted &&
                        (c.CurrentStatus == LoanCaseStatus.PendingLegalRawContract ||
                         c.CurrentStatus == LoanCaseStatus.PendingLegalFinalReview ||
                         c.CurrentStatus == LoanCaseStatus.PendingLegalFinalContract) &&
                        c.UpdatedAt != null)
            .Select(c => (now - c.UpdatedAt!.Value).TotalDays)
            .ToListAsync(ct);

        var legalSamples = legalInvestment.Concat(legalGuarantee).Concat(legalLoan).ToList();
        result.Add(new DepartmentBottleneckDto(
            "Legal", "حقوقی",
            legalSamples.Count == 0 ? 0 : Math.Round(legalSamples.Average(), 1),
            legalSamples.Count));

        var financeInvestment = await dbContext.InvestmentCases.AsNoTracking()
            .Where(c => !c.IsDeleted && c.CurrentPhase == CasePhase.Finance && c.UpdatedAt != null)
            .Select(c => (now - c.UpdatedAt!.Value).TotalDays)
            .ToListAsync(ct);

        var financeGuarantee = await dbContext.GuaranteeCases.AsNoTracking()
            .Where(c => !c.IsDeleted && c.CurrentPhase == GuaranteeCasePhase.Finance && c.UpdatedAt != null)
            .Select(c => (now - c.UpdatedAt!.Value).TotalDays)
            .ToListAsync(ct);

        var financeLoan = await dbContext.LoanCases.AsNoTracking()
            .Where(c => !c.IsDeleted &&
                        (c.CurrentStatus == LoanCaseStatus.PendingFinancialReview ||
                         c.CurrentStatus == LoanCaseStatus.RevisionRequestedByFinancial) &&
                        c.UpdatedAt != null)
            .Select(c => (now - c.UpdatedAt!.Value).TotalDays)
            .ToListAsync(ct);

        var financeSamples = financeInvestment.Concat(financeGuarantee).Concat(financeLoan).ToList();
        result.Add(new DepartmentBottleneckDto(
            "Financial", "مالی",
            financeSamples.Count == 0 ? 0 : Math.Round(financeSamples.Average(), 1),
            financeSamples.Count));

        var creditGuarantee = await dbContext.GuaranteeCases.AsNoTracking()
            .Where(c => !c.IsDeleted && c.CurrentPhase == GuaranteeCasePhase.CreditAssessment && c.UpdatedAt != null)
            .Select(c => (now - c.UpdatedAt!.Value).TotalDays)
            .ToListAsync(ct);

        var creditLoan = await dbContext.LoanCases.AsNoTracking()
            .Where(c => !c.IsDeleted &&
                        (c.CurrentStatus == LoanCaseStatus.PendingCreditReview ||
                         c.CurrentStatus == LoanCaseStatus.RevisionRequestedByCredit) &&
                        c.UpdatedAt != null)
            .Select(c => (now - c.UpdatedAt!.Value).TotalDays)
            .ToListAsync(ct);

        var creditSamples = creditGuarantee.Concat(creditLoan).ToList();
        result.Add(new DepartmentBottleneckDto(
            "Credit", "اعتبارات",
            creditSamples.Count == 0 ? 0 : Math.Round(creditSamples.Average(), 1),
            creditSamples.Count));

        return result;
    }

    private async Task<double> ComputeAverageReviewDaysAsync(CancellationToken ct)
    {
        var reviewStatuses = new[]
        {
            (int)CaseStatus.ReviewDataEntry1, (int)CaseStatus.ReviewDataEntry2,
            (int)CaseStatus.InitialValuation, (int)CaseStatus.SecondaryValuation,
            (int)CaseStatus.FinancialWorksheetReview, (int)CaseStatus.WaitingCeoApproval
        };

        var samples = await dbContext.InvestmentCases.AsNoTracking()
            .Where(c => !c.IsDeleted && reviewStatuses.Contains((int)c.CurrentStatus) && c.UpdatedAt != null)
            .Select(c => (c.UpdatedAt!.Value - c.CreatedAt).TotalDays)
            .Take(500)
            .ToListAsync(ct);

        return samples.Count == 0 ? 0 : Math.Round(samples.Average(), 1);
    }

    private async Task<double> ComputeRevisionRateAsync(string departmentKey, int queueCount, CancellationToken ct)
    {
        var since = DateTimeOffset.UtcNow.AddMonths(-6);
        var revisionCount = 0;

        if (departmentKey.Equals("Investment", StringComparison.OrdinalIgnoreCase))
        {
            revisionCount = await dbContext.CaseRevisions.AsNoTracking()
                .CountAsync(r => r.CreatedAt >= since, ct);
        }
        else if (departmentKey.Equals("Credit", StringComparison.OrdinalIgnoreCase))
        {
            revisionCount = await dbContext.LoanCaseWorkflowHistories.AsNoTracking()
                .CountAsync(h => h.CreatedAt >= since &&
                                 (h.ToStatus == LoanCaseStatus.RevisionRequestedByCredit ||
                                  h.Action.Contains("Revision")), ct);
            revisionCount += await dbContext.GuaranteeCaseWorkflowHistories.AsNoTracking()
                .CountAsync(h => h.CreatedAt >= since && h.Action.Contains("Revision"), ct);
        }
        else if (departmentKey.Equals("Legal", StringComparison.OrdinalIgnoreCase))
        {
            revisionCount = await dbContext.LoanCaseWorkflowHistories.AsNoTracking()
                .CountAsync(h => h.CreatedAt >= since &&
                                 (h.ToStatus == LoanCaseStatus.RevisionRequestedByLegal ||
                                  h.Action.Contains("Revision")), ct);
            revisionCount += await dbContext.CaseWorkflowHistories.AsNoTracking()
                .CountAsync(h => h.CreatedAt >= since && h.Action.Contains("Revision"), ct);
        }
        else if (departmentKey.Equals("Financial", StringComparison.OrdinalIgnoreCase))
        {
            revisionCount = await dbContext.LoanCaseWorkflowHistories.AsNoTracking()
                .CountAsync(h => h.CreatedAt >= since &&
                                 (h.ToStatus == LoanCaseStatus.RevisionRequestedByFinancial ||
                                  h.Action.Contains("Revision")), ct);
        }

        var denominator = Math.Max(queueCount, 1);
        return Math.Round(revisionCount * 100.0 / denominator, 1);
    }

    private sealed record InvestmentCaseProjection(
        Guid Id,
        string CaseNumber,
        int Status,
        int Phase,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);

    private static int CountPendingCases(
        IReadOnlyList<InvestmentCaseProjection> investments,
        IReadOnlyList<CaseStatusProjection> guarantees,
        IReadOnlyList<CaseStatusProjection> loans)
    {
        var count = investments.Count(c => !InvestmentTerminal.Contains(c.Status) && c.Status != (int)CaseStatus.Completed);
        count += guarantees.Count(c => !GuaranteeTerminal.Contains(c.Status) && c.Status != (int)GuaranteeCaseStatus.Completed);
        count += loans.Count(c => !LoanTerminal.Contains(c.Status) && c.Status != (int)LoanCaseStatus.Completed);
        return count;
    }

    private static int CountCompletedCases(
        IReadOnlyList<InvestmentCaseProjection> investments,
        IReadOnlyList<CaseStatusProjection> guarantees,
        IReadOnlyList<CaseStatusProjection> loans)
    {
        return investments.Count(c => c.Status == (int)CaseStatus.Completed)
            + guarantees.Count(c => c.Status == (int)GuaranteeCaseStatus.Completed)
            + loans.Count(c => c.Status == (int)LoanCaseStatus.Completed);
    }

    private static int CountCanceledCases(
        IReadOnlyList<InvestmentCaseProjection> investments,
        IReadOnlyList<CaseStatusProjection> guarantees,
        IReadOnlyList<CaseStatusProjection> loans)
    {
        return investments.Count(c => c.Status is (int)CaseStatus.Rejected or (int)CaseStatus.Cancelled)
            + guarantees.Count(c => c.Status is (int)GuaranteeCaseStatus.Rejected or (int)GuaranteeCaseStatus.Cancelled)
            + loans.Count(c => c.Status == (int)LoanCaseStatus.CanceledByCeo);
    }

    private static int ComputeProgressPercent(int status, int[] terminalStatuses, int completedStatus)
    {
        if (status == completedStatus)
            return 100;
        if (terminalStatuses.Contains(status))
            return 0;

        return Math.Clamp(status * 5, 5, 95);
    }
}
