using BuildingBlocks.Domain.Events;
using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Domain.Events;

public sealed record CasePhaseChangedDomainEvent(Guid CaseId, CasePhase FromPhase, CasePhase ToPhase)
    : DomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
