using BuildingBlocks.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BuildingBlocks.Persistence.Interceptors;

public sealed class DomainEventsDispatcherSaveChangesInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var entities = eventData.Context.ChangeTracker
            .Entries<Entity>()
            .Select(x => x.Entity)
            .Where(x => x.DomainEvents.Count != 0)
            .ToList();

        var domainEvents = entities.SelectMany(x => x.DomainEvents).ToList();
        entities.ForEach(x => x.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await publisher.Publish(domainEvent, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
