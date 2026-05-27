using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;

namespace Core.Domain.Entities;

/// <summary>
/// سقف اعتبار ضمانت‌نامه متقاضی — توسط مدیرعامل تعیین می‌شود (یک رکورد به ازای متقاضی حقیقی یا شرکت).
/// </summary>
public sealed class GuaranteeApplicantCreditProfile : Entity<Guid>, IAuditableEntity
{
    private GuaranteeApplicantCreditProfile()
    {
        ApplicantUserId = default!;
    }

    public GuaranteeApplicantCreditProfile(string applicantUserId, Guid? companyId, decimal creditLimitWithCheck, string setByUserId)
    {
        Id = Guid.NewGuid();
        ApplicantUserId = applicantUserId;
        CompanyId = companyId;
        CreditLimitWithCheck = creditLimitWithCheck;
        LastSetByUserId = setByUserId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public string ApplicantUserId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public decimal CreditLimitWithCheck { get; private set; }
    public string? LastSetByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void SetCreditLimit(decimal creditLimitWithCheck, string setByUserId)
    {
        CreditLimitWithCheck = creditLimitWithCheck;
        LastSetByUserId = setByUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
