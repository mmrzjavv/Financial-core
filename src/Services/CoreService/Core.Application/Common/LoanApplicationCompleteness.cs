using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Common;

public static class LoanApplicationCompleteness
{
    public static bool HasMinimumData(
        decimal? requestedAmount,
        string? facilitySubject,
        string? offeredGuarantees)
        => requestedAmount is > 0
           || !string.IsNullOrWhiteSpace(facilitySubject)
           || !string.IsNullOrWhiteSpace(offeredGuarantees);

    public static bool IsComplete(LoanCaseApplication? application)
    {
        if (application is null)
            return false;

        if (application.RequestedAmount is not > 0)
            return false;

        if (string.IsNullOrWhiteSpace(application.FacilitySubject))
            return false;

        if (string.IsNullOrWhiteSpace(application.OfferedGuarantees))
            return false;

        if (application.ApplicantCategory == ApplicantCategory.None)
            return false;

        return true;
    }
}
