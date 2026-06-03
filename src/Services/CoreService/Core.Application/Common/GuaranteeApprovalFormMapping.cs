using Core.Application.DTOs;
using Core.Application.Requests;
using Core.Domain.Entities;

namespace Core.Application.Common;

public static class GuaranteeApprovalFormMapping
{
    public static void Apply(
        GuaranteeApprovalForm form,
        GuaranteeCaseApplication? application,
        GuaranteeApplicantCreditSnapshotDto creditSnapshot,
        UpdateGuaranteeApprovalFormRequest? request = null)
    {
        form.Update(
            creditSnapshot.CreditLimitWithCheck,
            creditSnapshot.FundIssuedGuaranteesTotal,
            creditSnapshot.ActiveCommitments,
            creditSnapshot.RemainingCredit,
            First(request?.GuaranteeType, application?.GuaranteeType),
            First(request?.GuaranteeAmount, application?.RequestedGuaranteeAmount),
            FirstText(request?.GuaranteeAmountInWords, application?.BaseContractAmountInWords),
            FirstText(request?.ContractSubject, application?.ContractSubject),
            FirstText(request?.Beneficiary, application?.BeneficiaryName),
            First(request?.IssuanceDate, application?.ValidityFrom),
            First(request?.ExpiryDate, application?.ValidityTo),
            First(request?.ActiveDurationDays, application?.InitialValidityDays),
            request?.DepositRatePercent,
            request?.DepositAmount,
            request?.AnnualCommissionRatePercent,
            request?.CommissionAmount,
            FirstText(request?.CollateralDescription, application?.CollateralDescription),
            request?.GuarantorsDescription,
            request?.OtherNotes);
    }

    private static T? First<T>(T? primary, T? fallback) where T : struct
        => primary ?? fallback;

    private static string? FirstText(string? primary, string? fallback)
        => !string.IsNullOrWhiteSpace(primary) ? primary : fallback;
}
