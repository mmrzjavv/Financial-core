namespace Core.Application.Dashboard;

using Core.Application.DTOs;

public sealed record ModuleDashboardMetricsDto
{
    public string Module { get; init; } = default!;
    public string ModuleTitle { get; init; } = default!;
    public decimal ActiveVolume { get; init; }
    public int TotalCases { get; init; }
    public int ActiveCases { get; init; }
    public int CompletedCases { get; init; }
    public int PendingCeoApprovals { get; init; }
    public int RejectedCount { get; init; }
    public int CancelledCount { get; init; }
    public int ArchivedCount { get; init; }
    public double CompletionRate { get; init; }
    public double AverageDaysInReview { get; init; }
    public int QueueCount { get; init; }
    public IReadOnlyList<StatusCountDto> PipelineByStatus { get; init; } = [];
    public IReadOnlyList<MonthlyCountDto> MonthlyTrend { get; init; } = [];
    public IReadOnlyList<MonthlyFinancialOutputDto> MonthlyFinancialOutput { get; init; } = [];
    public IReadOnlyList<RecentActivityDto> RecentActivity { get; init; } = [];
}

public sealed record SystemHealthDto
{
    public int OnlineUsersCount { get; init; }
    public int DailyActiveUsers { get; init; }
    public int ActiveSessionsCount { get; init; }
}

public sealed record DepartmentSpecificMetricsDto
{
    public decimal TotalCommissions { get; init; }
    public decimal TotalRepayments { get; init; }
    public int OverdueInstallmentsCount { get; init; }
    public decimal OverdueAmount { get; init; }
    public int PendingFinancialReviews { get; init; }
    public int ContractsPendingReview { get; init; }
    public int CasesInLegalPhase { get; init; }
    public int PendingSignedContractUploads { get; init; }
    public int PendingCreditReviews { get; init; }
    public int RevisionCountLast6Months { get; init; }
    public int PendingValuations { get; init; }
    public int WaitingPaymentCount { get; init; }
    public int TechnicalReviewQueue { get; init; }
    public int ActiveFundCreditPools { get; init; }
}

public sealed record StatusBucketDto(string Category, string CategoryTitle, int Count);

public sealed record MonthlyFinancialOutputDto(int Year, int Month, decimal Amount, int CaseCount);

public sealed record DepartmentBottleneckDto(string Department, string DepartmentTitle, double AverageDays, int ActiveCaseCount);

public sealed record ModuleQueueCountDto(string Module, string ModuleTitle, int Count);

public sealed record InboxQuickLinkDto(
    Guid CaseId,
    string CaseNumber,
    string Module,
    string ModuleTitle,
    string StatusTitle,
    DateTimeOffset UpdatedAt);

public sealed record ApplicantCaseProgressDto(
    Guid CaseId,
    string CaseNumber,
    string Module,
    string ModuleTitle,
    int StatusCode,
    string StatusTitle,
    string PhaseTitle,
    int ProgressPercent,
    DateTimeOffset UpdatedAt);

public sealed record ApplicantRecentCommentDto(
    Guid CaseId,
    string CaseNumber,
    string Module,
    string ModuleTitle,
    string CommentText,
    string AuthorRole,
    DateTimeOffset CreatedAt);

public sealed record ExecutiveDashboardViewDto
{
    public IReadOnlyList<ModuleDashboardMetricsDto> Modules { get; init; } = [];
    public SystemHealthDto? SystemHealth { get; init; }
    public decimal ActiveGuaranteesVolume { get; init; }
    public decimal ActiveInvestmentsVolume { get; init; }
    public decimal ActiveLoansVolume { get; init; }
    public IReadOnlyList<StatusBucketDto> StatusDistribution { get; init; } = [];
    public IReadOnlyList<MonthlyFinancialOutputDto> MonthlyFinancialOutput { get; init; } = [];
    public int OnlineUsersCount { get; init; }
    public int DailyActiveUsers { get; init; }
    public IReadOnlyList<DepartmentBottleneckDto> DepartmentBottlenecks { get; init; } = [];
    public IReadOnlyList<StatusCountDto> PipelineByStatus { get; init; } = [];
    public IReadOnlyList<RecentActivityDto> RecentActivity { get; init; } = [];
    public int TotalActiveCases { get; init; }
    public int CompletedCases { get; init; }
    public int PendingCeoApprovals { get; init; }
    public int WaitingPaymentCount { get; init; }
    public int RejectedCount { get; init; }
    public double CompletionRate { get; init; }
    public decimal TotalRequestedAmount { get; init; }
    public decimal ApprovedPaymentsSum { get; init; }
    public int CasesThisMonth { get; init; }
    public double AverageDaysInReview { get; init; }
    public decimal ActivePipelineRequestedAmount { get; init; }
    public IReadOnlyList<StatusCountDto> CountsByPhase { get; init; } = [];
    public IReadOnlyList<MonthlyCountDto> MonthlyTrend { get; init; } = [];
    public int TotalCases { get; init; }
    public FundCreditLimitDashboardSectionDto? FundCreditLimits { get; init; }
}

public sealed record DepartmentDashboardViewDto
{
    public string DepartmentKey { get; init; } = default!;
    public string DepartmentTitle { get; init; } = default!;
    public int TotalQueueCount { get; init; }
    public IReadOnlyList<ModuleQueueCountDto> QueueByModule { get; init; } = [];
    public IReadOnlyList<ModuleDashboardMetricsDto> Modules { get; init; } = [];
    public DepartmentSpecificMetricsDto? SpecificMetrics { get; init; }
    public double RevisionRatePercent { get; init; }
    public IReadOnlyList<InboxQuickLinkDto> InboxQuickLinks { get; init; } = [];
}

public sealed record ApplicantDashboardViewDto
{
    public IReadOnlyList<ApplicantCaseProgressDto> ActiveCases { get; init; } = [];
    public int PendingActionsCount { get; init; }
    public decimal TotalRemainingDebt { get; init; }
    public int UnpaidInstallmentsCount { get; init; }
    public IReadOnlyList<ApplicantRecentCommentDto> RecentComments { get; init; } = [];
}

public sealed record RoleDashboardResponse
{
    public string ViewType { get; init; } = default!;
    public DateTimeOffset? ComputedAtUtc { get; init; }
    public bool IsStale { get; init; }
    public ExecutiveDashboardViewDto? Executive { get; init; }
    public DepartmentDashboardViewDto? Department { get; init; }
    public ApplicantDashboardViewDto? Applicant { get; init; }
    public FundCreditLimitDashboardSectionDto? FundCreditLimits { get; init; }
}

// Internal cache payloads (serialized to dashboard_stats_snapshots.PayloadJson)

public sealed record ExecutiveStatsCachePayload
{
    public ExecutiveDashboardViewDto Data { get; init; } = new();
}

public sealed record DepartmentStatsCachePayload
{
    public string DepartmentKey { get; init; } = default!;
    public string DepartmentTitle { get; init; } = default!;
    public int TotalQueueCount { get; init; }
    public IReadOnlyList<ModuleQueueCountDto> QueueByModule { get; init; } = [];
    public IReadOnlyList<ModuleDashboardMetricsDto> Modules { get; init; } = [];
    public DepartmentSpecificMetricsDto? SpecificMetrics { get; init; }
    public double RevisionRatePercent { get; init; }
}

public sealed record ApplicantStatsCachePayload
{
    public string UserId { get; init; } = default!;
    public IReadOnlyList<ApplicantCaseProgressDto> ActiveCases { get; init; } = [];
    public int PendingActionsCount { get; init; }
    public decimal TotalRemainingDebt { get; init; }
    public int UnpaidInstallmentsCount { get; init; }
    public IReadOnlyList<ApplicantRecentCommentDto> RecentComments { get; init; } = [];
}

public sealed record ApplicantOverviewSummaryDto
{
    public int ApplicantCount { get; init; }
    public int ActiveCasesCount { get; init; }
    public decimal TotalRemainingDebt { get; init; }
    public int UnpaidInstallmentsCount { get; init; }
    public IReadOnlyList<ModuleQueueCountDto> ActiveCasesByModule { get; init; } = [];
}

public sealed record AdminDashboardOverviewDto
{
    public DateTimeOffset? ComputedAtUtc { get; init; }
    public bool IsStale { get; init; }
    public IReadOnlyList<ModuleDashboardMetricsDto> Modules { get; init; } = [];
    public SystemHealthDto? SystemHealth { get; init; }
    public ExecutiveDashboardViewDto? Executive { get; init; }
    public CeoDashboardDto? Ceo { get; init; }
    public BoardDashboardDto? Board { get; init; }
    public IReadOnlyList<DepartmentDashboardViewDto> Departments { get; init; } = [];
    public ApplicantOverviewSummaryDto ApplicantSummary { get; init; } = new();
    public FundCreditLimitDashboardSectionDto? FundCreditLimits { get; init; }
}
