using BuildingBlocks.Domain.Entities;
using BuildingBlocks.Persistence.Db.DomainEvents;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Persistence.Db;

public abstract class DbContextBase : DbContext
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    protected DbContextBase(DbContextOptions options, IDomainEventDispatcher domainEventDispatcher) : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entitiesWithEvents = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Entity<Guid>)
            .Select(e => (Entity<Guid>)e.Entity)
            .Where(e => e.DomainEvents.Count != 0)
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entity in entitiesWithEvents)
            entity.ClearDomainEvents();

        if (domainEvents.Count != 0)
            await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        return result;
    }
}

