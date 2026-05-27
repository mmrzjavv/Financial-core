using BuildingBlocks.Persistence.Db;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Core.Persistence.Db;

internal static class CorePrincipalSoftDeleteQueryFilters
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyFilterForSoftDeletePrincipal<CaseComment, InvestmentCase>(e => e.Case);
        modelBuilder.ApplyFilterForSoftDeletePrincipal<CaseEvaluation, InvestmentCase>(e => e.Case);
        modelBuilder.ApplyFilterForSoftDeletePrincipal<CaseRevision, InvestmentCase>(e => e.Case);
        modelBuilder.ApplyFilterForSoftDeletePrincipal<CaseValuation, InvestmentCase>(e => e.Case);
        modelBuilder.ApplyFilterForSoftDeletePrincipal<CaseWorkflowHistory, InvestmentCase>(e => e.Case);
        modelBuilder.ApplyFilterForSoftDeletePrincipal<FinancialWorksheet, InvestmentCase>(e => e.Case);
        modelBuilder.ApplyFilterForSoftDeletePrincipal<InvestmentCaseDataEntry1, InvestmentCase>(e => e.Case);
        modelBuilder.ApplyFilterForSoftDeletePrincipal<InvestmentCaseDataEntry2, InvestmentCase>(e => e.Case);
        modelBuilder.ApplyFilterForSoftDeletePrincipal<PaymentRecord, InvestmentCase>(e => e.Case);
        modelBuilder.ApplyFilterForSoftDeletePrincipal<CaseDocument, InvestmentCase>(e => e.Case);

        modelBuilder.ApplyFilterForSoftDeletePrincipal<GuaranteeApprovalForm, GuaranteeCase>(e => e.Case);
        modelBuilder.ApplyFilterForSoftDeletePrincipal<GuaranteeCaseApplication, GuaranteeCase>(e => e.Case);
        modelBuilder.ApplyFilterForSoftDeletePrincipal<GuaranteeCaseComment, GuaranteeCase>(e => e.Case);
        modelBuilder.ApplyFilterForSoftDeletePrincipal<GuaranteeCaseWorkflowHistory, GuaranteeCase>(e => e.Case);
        modelBuilder.ApplyFilterForSoftDeletePrincipal<GuaranteeCaseDocument, GuaranteeCase>(e => e.Case);
        modelBuilder.ApplyFilterForSoftDeletePrincipal<GuaranteeRenewalCase, GuaranteeCase>(e => e.ParentGuaranteeCase);
    }
}
