using Core.Domain.Enums;

namespace Core.Domain.Entities.Analytics;

public sealed class DashboardStatsSnapshot
{
    public Guid Id { get; set; }

    /// <summary>Unique cache key, e.g. executive:global, department:Legal, applicant:{userId}.</summary>
    public string SnapshotKey { get; set; } = default!;

    public DashboardSnapshotType SnapshotType { get; set; }

    public string PayloadJson { get; set; } = default!;

    public DateTimeOffset ComputedAtUtc { get; set; }
}
