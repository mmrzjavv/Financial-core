using MediatR;

namespace BuildingBlocks.Domain.Events;

public interface IDomainEvent : INotification
{
    Guid Id { get; }
    DateTimeOffset OccurredAtUtc { get; }
}

