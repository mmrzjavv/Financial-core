using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities;

public sealed class LoanCaseApplication : Entity<Guid>, IAuditableEntity
{
    private LoanCaseApplication()
    {
    }

    public LoanCaseApplication(Guid caseId)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid CaseId { get; private set; }
    public LoanCase Case { get; private set; } = default!;

    public decimal? RequestedAmount { get; private set; }
    public string? RequestedAmountInWords { get; private set; }
    public string? FacilitySubject { get; private set; }
    public string? OfferedGuarantees { get; private set; }
    public ApplicantCategory ApplicantCategory { get; private set; }
    public string? ApplicantCategoryOther { get; private set; }
    public string? RepresentativePosition { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Update(
        decimal? requestedAmount,
        string? requestedAmountInWords,
        string? facilitySubject,
        string? offeredGuarantees,
        ApplicantCategory applicantCategory,
        string? applicantCategoryOther,
        string? representativePosition)
    {
        RequestedAmount = requestedAmount;
        RequestedAmountInWords = requestedAmountInWords;
        FacilitySubject = facilitySubject;
        OfferedGuarantees = offeredGuarantees;
        ApplicantCategory = applicantCategory;
        ApplicantCategoryOther = applicantCategoryOther;
        RepresentativePosition = representativePosition;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
