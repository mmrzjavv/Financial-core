using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities.Investment;

public sealed class InvestmentCaseApplicantProfile : Entity<Guid>, IAuditableEntity
{
    private InvestmentCaseApplicantProfile()
    {
        RepresentativeFullName = default!;
        ContactEmail = default!;
    }

    public InvestmentCaseApplicantProfile(
        Guid caseId,
        string representativeFullName,
        BusinessStage businessStage,
        string contactEmail,
        decimal requestedAmount)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        RepresentativeFullName = representativeFullName;
        BusinessStage = businessStage;
        ContactEmail = contactEmail;
        RequestedAmount = requestedAmount;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid CaseId { get; private set; }
    public InvestmentCase Case { get; private set; } = default!;

    /// <summary>نام و نام خانوادگی نماینده (غیر از پروفایل شرکت)</summary>
    public string RepresentativeFullName { get; private set; }

    public BusinessStage BusinessStage { get; private set; }

    public string ContactEmail { get; private set; }

    /// <summary>سرمایه مورد نیاز (ریال)</summary>
    public decimal RequestedAmount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Update(
        string representativeFullName,
        BusinessStage businessStage,
        string contactEmail,
        decimal requestedAmount)
    {
        RepresentativeFullName = representativeFullName;
        BusinessStage = businessStage;
        ContactEmail = contactEmail;
        RequestedAmount = requestedAmount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
