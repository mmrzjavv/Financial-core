using BuildingBlocks.Domain.Events;

namespace Core.Domain.Events;

public sealed record PaymentRecordedDomainEvent(Guid CaseId, Guid PaymentRecordId, decimal Amount)
    : DomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
