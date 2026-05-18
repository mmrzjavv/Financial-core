using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities;

public sealed class CaseValuation : Entity<Guid>, IAuditableEntity
{
    private CaseValuation()
    {
        Notes = default!;
        CreatedByUserId = default!;
    }

    public CaseValuation(Guid caseId, ValuationType type, decimal amount, string notes, string createdByUserId)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        Type = type;
        Amount = amount;
        Notes = notes;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid CaseId { get; private set; }
    public InvestmentCase Case { get; private set; } = default!;

    public ValuationType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Notes { get; private set; }
    public string CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
}

