using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class WorkflowHistoryConfiguration : IEntityTypeConfiguration<InvestmentCaseWorkflowHistory>
{
    public void Configure(EntityTypeBuilder<InvestmentCaseWorkflowHistory> builder)
    {
        builder.ToTable("case_workflow_history", DbSchemas.Investment);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.CaseId, x.CreatedAt });

        builder.Property(x => x.FromPhase).HasConversion<int>().IsRequired();
        builder.Property(x => x.ToPhase).HasConversion<int>().IsRequired();
        builder.Property(x => x.FromStatus).HasConversion<int>().IsRequired();
        builder.Property(x => x.ToStatus).HasConversion<int>().IsRequired();

        builder.Property(x => x.ChangedByUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ActorRole).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CorrelationId).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(4000);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}

