using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class LoanCaseWorkflowHistoryConfiguration : IEntityTypeConfiguration<LoanCaseWorkflowHistory>
{
    public void Configure(EntityTypeBuilder<LoanCaseWorkflowHistory> builder)
    {
        builder.ToTable("loan_case_workflow_history", DbSchemas.Loan);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CaseId);
        builder.HasIndex(x => x.CorrelationId);

        builder.Property(x => x.ChangedByUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ActorRole).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(4000);
        builder.Property(x => x.FromPhase).HasConversion<int>().IsRequired();
        builder.Property(x => x.ToPhase).HasConversion<int>().IsRequired();
        builder.Property(x => x.FromStatus).HasConversion<int>().IsRequired();
        builder.Property(x => x.ToStatus).HasConversion<int>().IsRequired();

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}
