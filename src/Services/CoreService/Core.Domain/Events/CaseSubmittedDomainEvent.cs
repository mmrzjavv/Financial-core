using BuildingBlocks.Domain.Events;

namespace Services.CoreService.Core.Domain.Events;

public sealed record CaseSubmittedDomainEvent(Guid CaseId, string SubmittedByUserId)
    : DomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
