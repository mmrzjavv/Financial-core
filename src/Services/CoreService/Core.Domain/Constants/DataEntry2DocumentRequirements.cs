using Core.Domain.Enums;

namespace Core.Domain.Constants;

public static class DataEntry2DocumentRequirements
{
    public static readonly DocumentType[] RequiredForSubmit =
    [
        DocumentType.CompanyIntroduction,
        DocumentType.EmployeeInsuranceList,
        DocumentType.TrialBalanceScan,
        DocumentType.TaxDocuments,
        DocumentType.ActivityLicenses,
        DocumentType.CompanyRegistration,
        DocumentType.CapitalRaisingPlans
    ];
}
