namespace Core.Application.Dashboard;

public sealed record StatusCountDto(int Status, string StatusTitle, int Count);

public sealed record MonthlyCountDto(int Year, int Month, int Count);

public sealed record RecentActivityDto(
    Guid CaseId,
    string CaseNumber,
    int FromStatus,
    int ToStatus,
    string Action,
    DateTimeOffset CreatedAt);

public sealed record CeoDashboardDto
{
    public IReadOnlyList<ModuleDashboardMetricsDto> Modules { get; init; } = [];
    public decimal TotalRiskExposure { get; init; }
    public IReadOnlyList<DepartmentBottleneckDto> DepartmentBottlenecks { get; init; } = [];
    public IReadOnlyList<StatusCountDto> PipelineByStatus { get; init; } = [];
    public decimal TotalRequestedAmount { get; init; }
    public decimal ApprovedPaymentsSum { get; init; }
    public int CasesThisMonth { get; init; }
    public double AverageDaysInReview { get; init; }
    public IReadOnlyList<StatusCountDto> TopStatuses { get; init; } = [];
    public IReadOnlyList<RecentActivityDto> RecentActivity { get; init; } = [];
    public int TotalActiveCases { get; init; }
    public int CompletedCases { get; init; }
    public int PendingCeoApprovals { get; init; }
    public int WaitingPaymentCount { get; init; }
    public int RejectedCount { get; init; }
    public double CompletionRate { get; init; }
    public decimal ActivePipelineRequestedAmount { get; init; }
}

public sealed record BoardDashboardDto
{
    public IReadOnlyList<ModuleDashboardMetricsDto> Modules { get; init; } = [];
    public IReadOnlyList<StatusCountDto> CountsByStatus { get; init; } = [];
    public IReadOnlyList<StatusCountDto> CountsByPhase { get; init; } = [];
    public IReadOnlyList<MonthlyCountDto> MonthlyTrend { get; init; } = [];
    public double CompletionRate { get; init; }
    public int TotalCases { get; init; }
    public decimal TotalActiveVolume { get; init; }
}
