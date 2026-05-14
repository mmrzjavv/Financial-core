using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Domain.Entities;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class Entity<TKey> : Entity
    where TKey : notnull
{
    public TKey Id { get; protected set; } = default!;
}
