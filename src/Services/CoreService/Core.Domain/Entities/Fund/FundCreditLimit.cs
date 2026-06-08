using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities.Fund;

/// <summary>
/// سقف اعتبار دوره‌ای صندوق — چند بازه غیرهمپوشان برای هر ماژول (ضمانت‌نامه / تسهیلات).
/// </summary>
public sealed class FundCreditLimit : Entity<Guid>, IAuditableEntity
{
    private FundCreditLimit()
    {
    }

    public FundCreditLimit(
        FundModuleType moduleType,
        decimal creditLimitWithCheck,
        DateOnly periodStart,
        DateOnly expiresAt,
        string setByUserId)
    {
        if (expiresAt < periodStart)
            throw new ArgumentException("تاریخ پایان سقف نمی‌تواند قبل از تاریخ شروع باشد.");

        Id = Guid.NewGuid();
        ModuleType = moduleType;
        CreditLimitWithCheck = creditLimitWithCheck;
        PeriodStart = periodStart;
        ExpiresAt = expiresAt;
        LastSetByUserId = setByUserId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public FundModuleType ModuleType { get; private set; }
    public decimal CreditLimitWithCheck { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly ExpiresAt { get; private set; }
    public string? LastSetByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public bool Overlaps(DateOnly periodStart, DateOnly expiresAt)
        => periodStart <= ExpiresAt && expiresAt >= PeriodStart;

    public bool IsActiveOn(DateOnly date) => date >= PeriodStart && date <= ExpiresAt;

    public void Update(
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
}
