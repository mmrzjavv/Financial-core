using BuildingBlocks.Domain.Events;
using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Domain.Events;

public sealed record RevisionRequestedDomainEvent(Guid CaseId, CasePhase Phase, string RequestedByUserId, int RevisionNumber = 0)
    : DomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
