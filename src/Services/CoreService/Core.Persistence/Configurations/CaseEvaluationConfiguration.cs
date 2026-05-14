using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Services.CoreService.Core.Domain.Entities;

namespace Services.CoreService.Core.Persistence.Configurations;

public sealed class CaseEvaluationConfiguration : IEntityTypeConfiguration<CaseEvaluation>
{
    public void Configure(EntityTypeBuilder<CaseEvaluation> builder)
    {
        builder.ToTable("case_evaluations", DbSchemas.Cases);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.CaseId, x.Phase, x.ReviewerUserId });

        builder.Property(x => x.Phase).HasConversion<int>().IsRequired();
        builder.Property(x => x.ReviewerUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ReviewerRole).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(8000);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Evaluation)
            .HasForeignKey(x => x.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}

