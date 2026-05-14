namespace BuildingBlocks.Domain.DomainEvents;

public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }
    public DateTimeOffset OccurredAt { get; }
}

