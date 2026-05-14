using BuildingBlocks.Domain.Events;

namespace Services.CoreService.Core.Domain.Events;

public sealed record CaseCompletedDomainEvent(Guid Id, DateTimeOffset OccurredAtUtc, Guid CaseId) : DomainEvent(Id, OccurredAtUtc);
