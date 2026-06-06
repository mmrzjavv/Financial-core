using Core.Domain.Entities;
using Core.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;


namespace Core.Application.Abstractions;

public interface ICoreDbContext
{
    DbSet<InvestmentCase> InvestmentCases { get; }
    DbSet<InvestmentCaseDataEntry1> DataEntry1 { get; }
    DbSet<InvestmentCaseDataEntry2> DataEntry2 { get; }
    DbSet<FinancialWorksheet> FinancialWorksheets { get; }
    DbSet<CaseDocument> CaseDocuments { get; }
    DbSet<CaseComment> CaseComments { get; }
    DbSet<CaseCommentAttachment> CaseCommentAttachments { get; }
    DbSet<CaseRevision> CaseRevisions { get; }
    DbSet<CaseEvaluation> CaseEvaluations { get; }
    DbSet<CaseEvaluationItem> CaseEvaluationItems { get; }
    DbSet<CaseValuation> CaseValuations { get; }
    DbSet<PaymentRecord> PaymentRecords { get; }
    DbSet<CaseWorkflowHistory> CaseWorkflowHistories { get; }
    DbSet<GuaranteeCase> GuaranteeCases { get; }
    DbSet<GuaranteeCaseApplication> GuaranteeCaseApplications { get; }
    DbSet<GuaranteeApprovalForm> GuaranteeApprovalForms { get; }
    DbSet<GuaranteeCaseDocument> GuaranteeCaseDocuments { get; }
    DbSet<GuaranteeCaseComment> GuaranteeCaseComments { get; }
    DbSet<GuaranteeCaseWorkflowHistory> GuaranteeCaseWorkflowHistories { get; }
    DbSet<GuaranteeRenewalCase> GuaranteeRenewalCases { get; }
    DbSet<GuaranteeApplicantCreditProfile> GuaranteeApplicantCreditProfiles { get; }
    DbSet<GuaranteeFundCreditLimit> GuaranteeFundCreditLimits { get; }
    DbSet<LoanCase> LoanCases { get; }
    DbSet<LoanCaseApplication> LoanCaseApplications { get; }
    DbSet<LoanApprovalDetail> LoanApprovalDetails { get; }
    DbSet<LoanCaseDocument> LoanCaseDocuments { get; }
    DbSet<LoanInstallment> LoanInstallments { get; }
    DbSet<LoanPayment> LoanPayments { get; }
    DbSet<LoanCaseComment> LoanCaseComments { get; }
    DbSet<LoanCaseWorkflowHistory> LoanCaseWorkflowHistories { get; }
    DbSet<User> Users { get; }
    DbSet<Company> Companies { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserSession> UserSessions { get; }

    Task<int> SaveChangesAsync(CancellationToken ct);
}