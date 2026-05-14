namespace BuildingBlocks.Domain.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

