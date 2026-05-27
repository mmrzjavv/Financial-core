using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class GuaranteeCaseWorkflowHistoryConfiguration : IEntityTypeConfiguration<GuaranteeCaseWorkflowHistory>
{
    public void Configure(EntityTypeBuilder<GuaranteeCaseWorkflowHistory> builder)
    {
        builder.ToTable("guarantee_case_workflow_history", DbSchemas.Cases);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CaseId);

        builder.Property(x => x.ChangedByUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ActorRole).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(4000);

        builder.Property(x => x.FromPhase).HasConversion<int>().IsRequired();
        builder.Property(x => x.ToPhase).HasConversion<int>().IsRequired();
        builder.Property(x => x.FromStatus).HasConversion<int>().IsRequired();
        builder.Property(x => x.ToStatus).HasConversion<int>().IsRequired();

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
    }
}
