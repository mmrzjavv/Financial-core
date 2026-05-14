using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Services.CoreService.Core.Domain.Entities;

namespace Services.CoreService.Core.Persistence.Configurations;

public sealed class CaseEvaluationItemConfiguration : IEntityTypeConfiguration<CaseEvaluationItem>
{
    public void Configure(EntityTypeBuilder<CaseEvaluationItem> builder)
    {
        builder.ToTable("case_evaluation_items");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.EvaluationId, x.Title }).IsUnique();

        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.IsApproved).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(4000);
    }
}

