using Core.Domain.Entities;

namespace Core.Application.Common;

public static class LoanApprovalDetailCompleteness
{
    public static bool IsComplete(LoanApprovalDetail? detail)
    {
        if (detail is null)
            return false;

        if (!detail.FacilityType.HasValue)
            return false;

        if (detail.ApprovedAmount is not > 0)
            return false;

        if (string.IsNullOrWhiteSpace(detail.ContractSubject))
            return false;

        if (detail.RepaymentMonths is not > 0)
            return false;

        if (detail.AnnualProfitRatePercent is null)
            return false;

        if (string.IsNullOrWhiteSpace(detail.CollateralDescription))
            return false;

        if (string.IsNullOrWhiteSpace(detail.GuarantorsDescription))
            return false;

        return true;
    }
}
