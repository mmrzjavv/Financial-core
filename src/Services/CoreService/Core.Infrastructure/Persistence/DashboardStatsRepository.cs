using Core.Application.Abstractions;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence;

public sealed class DashboardStatsRepository(CoreDbContext dbContext) : IDashboardStatsRepository
{
    public Task<DashboardStatsSnapshot?> GetByKeyAsync(string snapshotKey, CancellationToken cancellationToken = default)
        => dbContext.DashboardStatsSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SnapshotKey == snapshotKey, cancellationToken);

    public Task<IReadOnlyList<DashboardStatsSnapshot>> ListByTypeAsync(
        DashboardSnapshotType snapshotType,
        CancellationToken cancellationToken = default)
        => dbContext.DashboardStatsSnapshots
            .AsNoTracking()
            .Where(x => x.SnapshotType == snapshotType)
            .OrderByDescending(x => x.ComputedAtUtc)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DashboardStatsSnapshot>)t.Result, cancellationToken);

    public async Task UpsertAsync(DashboardStatsSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.DashboardStatsSnapshots
            .FirstOrDefaultAsync(x => x.SnapshotKey == snapshot.SnapshotKey, cancellationToken);

        if (existing is null)
        {
            dbContext.DashboardStatsSnapshots.Add(snapshot);
        }
        else
        {
            existing.PayloadJson = snapshot.PayloadJson;
            existing.ComputedAtUtc = snapshot.ComputedAtUtc;
            existing.SnapshotType = snapshot.SnapshotType;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
