namespace BuildingBlocks.Application.Abstractions;

public interface ICurrentUser
{
    string UserId { get; }
    IReadOnlySet<string> Roles { get; }
    string? CorrelationId { get; }
}

