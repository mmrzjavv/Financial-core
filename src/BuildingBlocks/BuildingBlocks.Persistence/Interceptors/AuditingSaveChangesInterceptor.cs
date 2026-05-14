using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BuildingBlocks.Persistence.Interceptors;

public sealed class AuditingSaveChangesInterceptor(IClock clock, IUserContext userContext) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = clock.UtcNow;
        foreach (var entry in eventData.Context.ChangeTracker.Entries<AuditableEntity>())
        {
            ApplyAudit(entry, now);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(EntityEntry<AuditableEntity> entry, DateTimeOffset now)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.GetType().GetProperty(nameof(AuditableEntity.CreatedAt))?.SetValue(entry.Entity, now);
                entry.Entity.GetType().GetProperty(nameof(AuditableEntity.CreatedBy))?.SetValue(entry.Entity, userContext.UserId);
                break;
            case EntityState.Modified:
                entry.Entity.GetType().GetProperty(nameof(AuditableEntity.UpdatedAt))?.SetValue(entry.Entity, now);
                entry.Entity.GetType().GetProperty(nameof(AuditableEntity.UpdatedBy))?.SetValue(entry.Entity, userContext.UserId);
                break;
        }
    }
}

