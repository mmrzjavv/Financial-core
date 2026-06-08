namespace Core.Application.Dashboard;

public enum EmployeeKpiPeriod
{
    Last30Days,
    Last90Days,
    ThisQuarter,
    AllTime
}

public sealed record EmployeeKpiMetricsDto
{
    public string UserId { get; init; } = default!;
    public string? FullName { get; init; }
    public double AverageResolutionHours { get; init; }
    public double AverageResolutionDays { get; init; }
    public double MinResolutionHours { get; init; }
    public double MaxResolutionHours { get; init; }
    public int TotalTasksResolved { get; init; }
    public IReadOnlyList<double> ResolutionHoursSamples { get; init; } = [];
}

public sealed record DepartmentEmployeeKpiDto
{
    public string DepartmentKey { get; init; } = default!;
    public string DepartmentTitle { get; init; } = default!;
    public IReadOnlyList<EmployeeKpiMetricsDto> Employees { get; init; } = [];
}

public sealed record EmployeeKpiPeriodSnapshotDto
{
    public string Period { get; init; } = default!;
    public DateTimeOffset PeriodStartUtc { get; init; }
    public DateTimeOffset PeriodEndUtc { get; init; }
    public IReadOnlyList<DepartmentEmployeeKpiDto> Departments { get; init; } = [];
}

public sealed record EmployeeKpiCachePayload
{
    public IReadOnlyList<EmployeeKpiPeriodSnapshotDto> Periods { get; init; } = [];
}

public sealed record EmployeeKpiJobRunResultDto
{
    public DateTimeOffset ComputedAtUtc { get; init; }
    public string Message { get; init; } = "Employee KPI aggregation completed.";
}

public sealed record EmployeeKpiResponseDto
{
    public string Period { get; init; } = default!;
    public DateTimeOffset ComputedAtUtc { get; init; }
    public DateTimeOffset PeriodStartUtc { get; init; }
    public DateTimeOffset PeriodEndUtc { get; init; }
    public bool IsStale { get; init; }
    public IReadOnlyList<DepartmentEmployeeKpiDto> Departments { get; init; } = [];
}
