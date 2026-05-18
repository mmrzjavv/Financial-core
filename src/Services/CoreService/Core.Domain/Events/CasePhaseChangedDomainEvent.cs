using BuildingBlocks.Domain.Events;
using Core.Domain.Enums;

namespace Core.Domain.Events;

public sealed record CasePhaseChangedDomainEvent(Guid CaseId, CasePhase FromPhase, CasePhase ToPhase)
    : DomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
