using Core.Application.Abstractions;
using Core.Application.Requests;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Common;

internal static class GuaranteeCaseApplicationPersistence
{
    public static async Task<GuaranteeCaseApplication> UpsertAsync(
        ICoreDbContext dbContext,
        Guid caseId,
        UpdateGuaranteeApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var application = await dbContext.GuaranteeCaseApplications
            .FirstOrDefaultAsync(x => x.CaseId == caseId, cancellationToken);

        if (application is null)
        {
            application = new GuaranteeCaseApplication(caseId);
            await dbContext.GuaranteeCaseApplications.AddAsync(application, cancellationToken);
        }

        application.Update(
            request.GuaranteeType,
            request.ContractSubject,
            request.IsKnowledgeBasedProduct,
            request.BeneficiaryName,
            request.BeneficiaryNationalId,
            request.BeneficiaryCompanyType,
            request.ApplicantCategory,
            request.ApplicantCategoryOther,
            request.ApplicantLegalForm,
            request.BaseContractNumber,
            request.BaseContractAmount,
            request.BaseContractAmountInWords,
            request.PriceAdjustmentRatePercent,
            request.ExecutionProvince,
            request.RequestedGuaranteeAmount,
            request.InitialValidityDays,
            request.ValidityFrom,
            request.ValidityTo,
            request.CollateralDescription,
            request.FacilitySubject);

        return application;
    }

    public static Task<GuaranteeCaseApplication?> GetByCaseIdAsync(
        ICoreDbContext dbContext,
        Guid caseId,
        CancellationToken cancellationToken)
        => dbContext.GuaranteeCaseApplications
            .FirstOrDefaultAsync(x => x.CaseId == caseId, cancellationToken);
}
