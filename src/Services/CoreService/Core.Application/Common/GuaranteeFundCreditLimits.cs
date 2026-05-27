namespace Core.Application.Common;

public static class GuaranteeFundCreditLimits
{
    /// <summary>حداکثر مطابق numeric(18,2) در PostgreSQL.</summary>
    public const decimal MaxCreditLimitWithCheck = 9_999_999_999_999_999.99m;
}
