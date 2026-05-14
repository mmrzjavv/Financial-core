namespace BuildingBlocks.Domain.Abstractions;

public interface IUserContext
{
    string? UserId { get; }
    string? UserName { get; }
    IReadOnlyCollection<string> Roles { get; }
}

