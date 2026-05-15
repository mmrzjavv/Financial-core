namespace Core.Application.Abstractions;

public interface ICurrentUserAccessor
{
    Guid? UserId { get; }
    Guid? SessionId { get; }
}
