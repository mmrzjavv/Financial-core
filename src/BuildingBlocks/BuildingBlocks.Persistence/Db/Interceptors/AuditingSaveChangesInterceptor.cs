using BuildingBlocks.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BuildingBlocks.Persistence.Db.Interceptors;

public sealed class AuditingSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IClock _clock;

    public AuditingSaveChangesInterceptor(IClock clock) => _clock = clock;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var db = eventData.Context;
        if (db is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = _clock.UtcNow;

        foreach (var entry in db.ChangeTracker.Entries())
        {
            if (entry.Entity is not IAuditableEntity)
                continue;

            if (entry.State == EntityState.Added)
            {
                var createdAt = entry.Property(nameof(IAuditableEntity.CreatedAt));
                if (createdAt.Metadata.ClrType == typeof(DateTimeOffset))
                    createdAt.CurrentValue = now;
            }

            if (entry.State == EntityState.Modified)
            {
                var updatedAt = entry.Property(nameof(IAuditableEntity.UpdatedAt));
                if (updatedAt.Metadata.ClrType == typeof(DateTimeOffset?))
                    updatedAt.CurrentValue = now;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
