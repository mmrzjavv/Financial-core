namespace BuildingBlocks.Domain.Events;

public abstract record DomainEvent(Guid Id, DateTimeOffset OccurredAtUtc) : IDomainEvent
{
    public static T Create<T>(Func<Guid, DateTimeOffset, T> factory, DateTimeOffset occurredAtUtc)
        where T : DomainEvent => factory(Guid.NewGuid(), occurredAtUtc);
}

