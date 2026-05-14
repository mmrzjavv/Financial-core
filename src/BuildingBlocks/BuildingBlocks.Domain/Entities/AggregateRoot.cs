namespace BuildingBlocks.Domain.Entities;

public abstract class AggregateRoot : Entity;

public abstract class AggregateRoot<TKey> : Entity<TKey>
    where TKey : notnull;
