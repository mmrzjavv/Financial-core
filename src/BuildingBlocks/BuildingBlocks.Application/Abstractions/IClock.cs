namespace BuildingBlocks.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

