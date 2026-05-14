using MediatR;

namespace BuildingBlocks.Domain.DomainEvents;

public interface IDomainEvent : INotification
{
    Guid Id { get; }
    DateTimeOffset OccurredAt { get; }
}
