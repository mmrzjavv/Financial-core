namespace Core.Application.Dashboard;

public static class EmployeeKpiPeriodResolver
{
    public const string SnapshotKey = "employee-kpi:global";

    public static (DateTimeOffset Start, DateTimeOffset End) ResolveRange(EmployeeKpiPeriod period, DateTimeOffset nowUtc)
    {
        var end = nowUtc;
        return period switch
        {
            EmployeeKpiPeriod.Last30Days => (end.AddDays(-30), end),
            EmployeeKpiPeriod.Last90Days => (end.AddDays(-90), end),
            EmployeeKpiPeriod.ThisQuarter => (StartOfQuarter(end), end),
            EmployeeKpiPeriod.AllTime => (DateTimeOffset.MinValue, end),
            _ => (end.AddDays(-30), end)
        };
    }

    public static string ToApiValue(EmployeeKpiPeriod period) => period switch
    {
        EmployeeKpiPeriod.Last30Days => "Last30Days",
        EmployeeKpiPeriod.Last90Days => "Last90Days",
        EmployeeKpiPeriod.ThisQuarter => "ThisQuarter",
        EmployeeKpiPeriod.AllTime => "AllTime",
        _ => "Last30Days"
    };

    public static bool TryParse(string? value, out EmployeeKpiPeriod period)
    {
        period = EmployeeKpiPeriod.Last30Days;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        return Enum.TryParse(value, ignoreCase: true, out period);
    }

    private static DateTimeOffset StartOfQuarter(DateTimeOffset value)
    {
        var quarterMonth = ((value.Month - 1) / 3) * 3 + 1;
        return new DateTimeOffset(value.Year, quarterMonth, 1, 0, 0, 0, TimeSpan.Zero);
    }
}
