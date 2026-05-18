using BuildingBlocks.Application.Results;
using Core.Application.Abstractions;
using Core.Application.Kanban;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Dashboard;

public sealed class ExecutiveDashboardService(ICoreDbContext dbContext) : IExecutiveDashboardService
{
    private static readonly int[] TerminalStatuses =
    [
        (int)CaseStatus.Completed,
        (int)CaseStatus.Rejected,
        (int)CaseStatus.Cancelled,
        (int)CaseStatus.Archived
    ];

    private static readonly int[] ReviewStatuses =
    [
        (int)CaseStatus.ReviewDataEntry1,
        (int)CaseStatus.ReviewDataEntry2,
        (int)CaseStatus.InitialValuation,
        (int)CaseStatus.SecondaryValuation,
        (int)CaseStatus.FinancialWorksheetReview,
        (int)CaseStatus.WaitingCeoApproval
    ];

    public async Task<Result<CeoDashboardDto>> GetCeoDashboardAsync(CancellationToken cancellationToken = default)
    {
        var monthStart = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var pipeline = await dbContext.InvestmentCases
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .GroupBy(c => c.CurrentStatus)
            .Select(g => new { Status = (int)g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var pipelineByStatus = pipeline
            .Select(x => new StatusCountDto(x.Status, CaseKanbanRules.GetStatusTitle((CaseStatus)x.Status), x.Count))
            .OrderByDescending(x => x.Count)
            .ToList();

        var totalRequested = await dbContext.DataEntry1
            .AsNoTracking()
            .Where(d => !d.Case.IsDeleted)
            .SumAsync(d => (decimal?)d.RequestedAmount, cancellationToken) ?? 0m;

        var approvedPayments = await dbContext.PaymentRecords
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Completed && !p.Case.IsDeleted)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var casesThisMonth = await dbContext.InvestmentCases
            .AsNoTracking()
            .CountAsync(c => !c.IsDeleted && c.CreatedAt >= monthStart, cancellationToken);

        var completedCount = pipeline
            .Where(x => x.Status == (int)CaseStatus.Completed)
            .Sum(x => x.Count);

        var rejectedCount = pipeline
            .Where(x => x.Status == (int)CaseStatus.Rejected)
            .Sum(x => x.Count);

        var pendingCeo = pipeline
            .Where(x => x.Status == (int)CaseStatus.WaitingCeoApproval)
            .Sum(x => x.Count);

        var waitingPayment = pipeline
            .Where(x => x.Status == (int)CaseStatus.WaitingPayment)
            .Sum(x => x.Count);

        var activeCount = pipeline
            .Where(x => !TerminalStatuses.Contains(x.Status))
            .Sum(x => x.Count);

        var totalCases = pipeline.Sum(x => x.Count);
        var completionRate = totalCases == 0 ? 0 : Math.Round(completedCount * 100.0 / totalCases, 1);

        var activePipelineRequested = await dbContext.DataEntry1
            .AsNoTracking()
            .Where(d => !d.Case.IsDeleted && !TerminalStatuses.Contains((int)d.Case.CurrentStatus))
            .SumAsync(d => (decimal?)d.RequestedAmount, cancellationToken) ?? 0m;

        var avgReviewDays = await ComputeAverageReviewDaysAsync(cancellationToken);

        var recent = await dbContext.CaseWorkflowHistories
            .AsNoTracking()
            .OrderByDescending(h => h.CreatedAt)
            .Take(15)
            .Select(h => new RecentActivityDto(
                h.CaseId,
                h.Case.CaseNumber,
                (int)h.FromStatus,
                (int)h.ToStatus,
                h.Action,
                h.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<CeoDashboardDto>.Ok(new CeoDashboardDto
        {
            PipelineByStatus = pipelineByStatus,
            TotalRequestedAmount = totalRequested,
            ApprovedPaymentsSum = approvedPayments,
            CasesThisMonth = casesThisMonth,
            AverageDaysInReview = avgReviewDays,
            TopStatuses = pipelineByStatus.Take(8).ToList(),
            RecentActivity = recent,
            TotalActiveCases = activeCount,
            CompletedCases = completedCount,
            PendingCeoApprovals = pendingCeo,
            WaitingPaymentCount = waitingPayment,
            RejectedCount = rejectedCount,
            CompletionRate = completionRate,
            ActivePipelineRequestedAmount = activePipelineRequested
        });
    }

    public async Task<Result<BoardDashboardDto>> GetBoardDashboardAsync(CancellationToken cancellationToken = default)
    {
        var statusCounts = await dbContext.InvestmentCases
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .GroupBy(c => c.CurrentStatus)
            .Select(g => new { Status = (int)g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var phaseCounts = await dbContext.InvestmentCases
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .GroupBy(c => c.CurrentPhase)
            .Select(g => new { Phase = (int)g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var sixMonthsAgo = DateTimeOffset.UtcNow.AddMonths(-6);
        var monthlyRaw = await dbContext.InvestmentCases
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.CreatedAt >= sixMonthsAgo)
            .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        var total = statusCounts.Sum(x => x.Count);
        var completed = statusCounts
            .Where(x => x.Status == (int)CaseStatus.Completed)
            .Sum(x => x.Count);

        return Result<BoardDashboardDto>.Ok(new BoardDashboardDto
        {
            CountsByStatus = statusCounts
                .Select(x => new StatusCountDto(x.Status, CaseKanbanRules.GetStatusTitle((CaseStatus)x.Status), x.Count))
                .OrderByDescending(x => x.Count)
                .ToList(),
            CountsByPhase = phaseCounts
                .Select(x => new StatusCountDto(x.Phase, CaseKanbanRules.GetPhaseTitle((CasePhase)x.Phase), x.Count))
                .OrderByDescending(x => x.Count)
                .ToList(),
            MonthlyTrend = monthlyRaw
                .Select(x => new MonthlyCountDto(x.Year, x.Month, x.Count))
                .ToList(),
            CompletionRate = total == 0 ? 0 : Math.Round(completed * 100.0 / total, 1),
            TotalCases = total
        });
    }

    private async Task<double> ComputeAverageReviewDaysAsync(CancellationToken cancellationToken)
    {
        var samples = await dbContext.InvestmentCases
            .AsNoTracking()
            .Where(c => !c.IsDeleted && ReviewStatuses.Contains((int)c.CurrentStatus) && c.UpdatedAt != null)
            .Select(c => (c.UpdatedAt!.Value - c.CreatedAt).TotalDays)
            .Take(500)
            .ToListAsync(cancellationToken);

        return samples.Count == 0 ? 0 : Math.Round(samples.Average(), 1);
    }
}
