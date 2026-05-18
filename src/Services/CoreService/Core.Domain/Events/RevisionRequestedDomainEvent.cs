using BuildingBlocks.Domain.Events;
using Core.Domain.Enums;

namespace Core.Domain.Events;

public sealed record RevisionRequestedDomainEvent(Guid CaseId, CasePhase Phase, string RequestedByUserId, int RevisionNumber = 0)
    : DomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
