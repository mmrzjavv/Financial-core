using BuildingBlocks.Domain.Events;

namespace Services.CoreService.Core.Domain.Events;

public sealed record PaymentRecordedDomainEvent(Guid CaseId, Guid PaymentRecordId, decimal Amount)
    : DomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
