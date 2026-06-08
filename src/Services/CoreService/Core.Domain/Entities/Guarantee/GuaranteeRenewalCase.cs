using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities.Guarantee;

public sealed class GuaranteeRenewalCase : AggregateRoot<Guid>, IAuditableEntity, ISoftDelete
{
    private GuaranteeRenewalCase()
    {
        CaseNumber = default!;
        ApplicantUserId = default!;
    }

    public GuaranteeRenewalCase(
        string caseNumber,
        string applicantUserId,
        Guid parentGuaranteeCaseId,
        RenewalKind renewalKind,
        DateOnly? requestedExpiryDate,
        decimal? requestedAmount)
    {
        Id = Guid.NewGuid();
        CaseNumber = caseNumber;
        ApplicantUserId = applicantUserId;
        ParentGuaranteeCaseId = parentGuaranteeCaseId;
        RenewalKind = renewalKind;
        RequestedExpiryDate = requestedExpiryDate;
        RequestedAmount = requestedAmount;
        CurrentStatus = GuaranteeRenewalStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string CaseNumber { get; private set; }
    public string ApplicantUserId { get; private set; }
    public Guid ParentGuaranteeCaseId { get; private set; }
    public GuaranteeCase ParentGuaranteeCase { get; private set; } = default!;

    public RenewalKind RenewalKind { get; private set; }
    public GuaranteeRenewalStatus CurrentStatus { get; private set; }
    public string? WorkflowInstanceId { get; private set; }

    public DateOnly? RequestedExpiryDate { get; private set; }
    public decimal? RequestedAmount { get; private set; }
    public DateOnly? ApprovedExpiryDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public void AttachWorkflowInstance(string workflowInstanceId)
    {
        WorkflowInstanceId = workflowInstanceId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void TransitionTo(GuaranteeRenewalStatus nextStatus, string changedByUserId)
    {
        CurrentStatus = nextStatus;
        UpdatedAt = DateTimeOffset.UtcNow;

        if (nextStatus == GuaranteeRenewalStatus.Completed)
            CompletedAt = DateTimeOffset.UtcNow;
    }

    public void SetApprovedExpiryDate(DateOnly date)
    {
        ApprovedExpiryDate = date;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
