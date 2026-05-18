using BuildingBlocks.Domain.Events;

namespace Core.Domain.Events;

public sealed record CaseCompletedDomainEvent(Guid Id, DateTimeOffset OccurredAtUtc, Guid CaseId) : DomainEvent(Id, OccurredAtUtc);
