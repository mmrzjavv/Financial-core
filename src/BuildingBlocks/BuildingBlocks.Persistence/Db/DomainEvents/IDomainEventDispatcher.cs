using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Persistence.Db.DomainEvents;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken ct);
}
