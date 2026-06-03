using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Common;

public static class GuaranteeApplicationCompleteness
{
    public static bool HasMinimumData(
        GuaranteeType? guaranteeType,
        string? contractSubject,
        decimal? requestedGuaranteeAmount)
        => guaranteeType.HasValue
           || !string.IsNullOrWhiteSpace(contractSubject)
           || requestedGuaranteeAmount is > 0;

    public static bool IsComplete(GuaranteeCaseApplication? application)
    {
        if (application is null)
            return false;

        if (!application.GuaranteeType.HasValue)
            return false;

        if (string.IsNullOrWhiteSpace(application.ContractSubject))
            return false;

        if (string.IsNullOrWhiteSpace(application.BeneficiaryName))
            return false;

        if (application.RequestedGuaranteeAmount is not > 0)
            return false;

        if (application.ApplicantCategory == ApplicantCategory.None)
            return false;

        return true;
    }
}
