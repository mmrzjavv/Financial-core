using Core.Domain.Entities;

namespace Core.Application.Common;

public static class GuaranteeApprovalFormCompleteness
{
    public static bool IsComplete(GuaranteeApprovalForm? form)
    {
        if (form is null)
            return false;

        if (!form.GuaranteeType.HasValue)
            return false;

        if (form.GuaranteeAmount is not > 0)
            return false;

        if (string.IsNullOrWhiteSpace(form.ContractSubject))
            return false;

        if (string.IsNullOrWhiteSpace(form.Beneficiary))
            return false;

        return true;
    }
}
