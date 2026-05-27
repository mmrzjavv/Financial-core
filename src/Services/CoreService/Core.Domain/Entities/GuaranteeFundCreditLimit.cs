using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;

namespace Core.Domain.Entities;

/// <summary>
/// سقف اعتبار کل صندوق برای صدور ضمانت‌نامه — یک رکورد فعال؛ صادره و تعهدات فقط در بازه PeriodStart تا ExpiresAt محاسبه می‌شوند.
/// </summary>
public sealed class GuaranteeFundCreditLimit : Entity<Guid>, IAuditableEntity
{
    public static readonly Guid SingletonId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private GuaranteeFundCreditLimit()
    {
    }

    public GuaranteeFundCreditLimit(
        decimal creditLimitWithCheck,
        DateOnly periodStart,
        DateOnly expiresAt,
        string setByUserId)
    {
        if (expiresAt < periodStart)
            throw new ArgumentException("تاریخ پایان سقف نمی‌تواند قبل از تاریخ شروع باشد.");

        Id = SingletonId;
        CreditLimitWithCheck = creditLimitWithCheck;
        PeriodStart = periodStart;
        ExpiresAt = expiresAt;
        LastSetByUserId = setByUserId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public decimal CreditLimitWithCheck { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly ExpiresAt { get; private set; }
    public string? LastSetByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void SetCreditLimit(
        decimal creditLimitWithCheck,
        DateOnly periodStart,
        DateOnly expiresAt,
        string setByUserId)
    {
        if (expiresAt < periodStart)
            throw new ArgumentException("تاریخ پایان سقف نمی‌تواند قبل از تاریخ شروع باشد.");

        CreditLimitWithCheck = creditLimitWithCheck;
        PeriodStart = periodStart;
        ExpiresAt = expiresAt;
        LastSetByUserId = setByUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActiveOn(DateOnly date) => date >= PeriodStart && date <= ExpiresAt;
}
