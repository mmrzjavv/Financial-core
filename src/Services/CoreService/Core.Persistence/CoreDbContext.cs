using BuildingBlocks.Persistence.Db;
using BuildingBlocks.Persistence.Db.DomainEvents;
using BuildingBlocks.Persistence.Db.Interceptors;
using Core.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Services.CoreService.Core.Domain.Entities;
using Services.CoreService.Core.Domain.Identity.Entities;

namespace Services.CoreService.Core.Persistence;

public sealed class CoreDbContext : DbContextBase, ICoreDbContext
{
    public CoreDbContext(
        DbContextOptions<CoreDbContext> options,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options, domainEventDispatcher)
    {
    }

    public DbSet<InvestmentCase> InvestmentCases => Set<InvestmentCase>();
    public DbSet<InvestmentCaseDataEntry1> DataEntry1 => Set<InvestmentCaseDataEntry1>();
    public DbSet<InvestmentCaseDataEntry2> DataEntry2 => Set<InvestmentCaseDataEntry2>();
    public DbSet<FinancialWorksheet> FinancialWorksheets => Set<FinancialWorksheet>();
    public DbSet<CaseDocument> CaseDocuments => Set<CaseDocument>();
    public DbSet<CaseComment> CaseComments => Set<CaseComment>();
    public DbSet<CaseRevision> CaseRevisions => Set<CaseRevision>();
    public DbSet<CaseEvaluation> CaseEvaluations => Set<CaseEvaluation>();
    public DbSet<CaseEvaluationItem> CaseEvaluationItems => Set<CaseEvaluationItem>();
    public DbSet<CaseValuation> CaseValuations => Set<CaseValuation>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<CaseWorkflowHistory> CaseWorkflowHistories => Set<CaseWorkflowHistory>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreDbContext).Assembly);
        modelBuilder.ApplySoftDeleteQueryFilter();

        // Legacy migrations mapped RowVersion -> xmin; strip if any convention reintroduces it.
        var rowVersion = modelBuilder.Entity<InvestmentCase>().Metadata.FindProperty("RowVersion");
        if (rowVersion is not null)
            modelBuilder.Entity<InvestmentCase>().Metadata.RemoveProperty(rowVersion);
    }
}
