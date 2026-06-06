using Core.Application.Abstractions;
using Core.Application.Requests;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Common;

public static class LoanCaseApplicationPersistence
{
    public static async Task<LoanCaseApplication> UpsertAsync(
        ICoreDbContext dbContext,
        Guid caseId,
        UpdateLoanApplicationRequest request,
        CancellationToken ct)
    {
        var application = await dbContext.LoanCaseApplications
            .FirstOrDefaultAsync(x => x.CaseId == caseId, ct);

        if (application is null)
        {
            application = new LoanCaseApplication(caseId);
            await dbContext.LoanCaseApplications.AddAsync(application, ct);
        }

        application.Update(
            request.RequestedAmount,
            request.RequestedAmountInWords,
            request.FacilitySubject,
            request.OfferedGuarantees,
            request.ApplicantCategory,
            request.ApplicantCategoryOther,
            request.RepresentativePosition);

        return application;
    }
}
