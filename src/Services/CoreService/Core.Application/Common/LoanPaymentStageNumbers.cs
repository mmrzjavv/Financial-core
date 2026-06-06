namespace Core.Application.Common;

public static class LoanPaymentStageNumbers
{
    /// <summary>Disbursement stages use 1..N; repayment stages use base + installment row.</summary>
    public const int RepaymentBase = 10_000;

    public static int ForInstallmentRepayment(int installmentRowNumber) => RepaymentBase + installmentRowNumber;

    public static bool IsRepaymentStage(int stageNumber) => stageNumber >= RepaymentBase;
}
