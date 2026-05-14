using Microsoft.EntityFrameworkCore;
using Services.CoreService.Core.Domain.Entities;
using Services.CoreService.Core.Domain.Identity.Entities;

namespace Core.Application.Abstractions;

public interface ICoreDbContext
{
    DbSet<InvestmentCase> InvestmentCases { get; }
    DbSet<InvestmentCaseDataEntry1> DataEntry1 { get; }
    DbSet<InvestmentCaseDataEntry2> DataEntry2 { get; }
    DbSet<FinancialWorksheet> FinancialWorksheets { get; }
    DbSet<CaseDocument> CaseDocuments { get; }
    DbSet<CaseComment> CaseComments { get; }
    DbSet<CaseRevision> CaseRevisions { get; }
    DbSet<CaseEvaluation> CaseEvaluations { get; }
    DbSet<CaseEvaluationItem> CaseEvaluationItems { get; }
    DbSet<CaseValuation> CaseValuations { get; }
    DbSet<PaymentRecord> PaymentRecords { get; }
    DbSet<CaseWorkflowHistory> CaseWorkflowHistories { get; }
    DbSet<User> Users { get; }
    DbSet<Company> Companies { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserSession> UserSessions { get; }

    Task<int> SaveChangesAsync(CancellationToken ct);
}
