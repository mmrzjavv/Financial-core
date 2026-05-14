namespace BuildingBlocks.Domain.Abstractions;

public interface ISoftDelete
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAt { get; }
}

