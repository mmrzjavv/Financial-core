using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Abstractions;

public interface IDashboardStatsRepository
{
    Task<DashboardStatsSnapshot?> GetByKeyAsync(string snapshotKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardStatsSnapshot>> ListByTypeAsync(
        DashboardSnapshotType snapshotType,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(DashboardStatsSnapshot snapshot, CancellationToken cancellationToken = default);
}
