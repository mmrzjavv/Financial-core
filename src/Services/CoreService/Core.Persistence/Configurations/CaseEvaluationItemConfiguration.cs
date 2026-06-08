using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class CaseEvaluationItemConfiguration : IEntityTypeConfiguration<InvestmentCaseEvaluationItem>
{
    public void Configure(EntityTypeBuilder<InvestmentCaseEvaluationItem> builder)
    {
        builder.ToTable("case_evaluation_items", DbSchemas.Investment);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.EvaluationId, x.Title }).IsUnique();

        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.IsApproved).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(4000);
    }
}

