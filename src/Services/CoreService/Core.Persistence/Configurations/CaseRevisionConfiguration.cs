using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class CaseRevisionConfiguration : IEntityTypeConfiguration<CaseRevision>
{
    public void Configure(EntityTypeBuilder<CaseRevision> builder)
    {
        builder.ToTable("case_revisions", DbSchemas.Investment);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.CaseId, x.Phase, x.RevisionNumber }).IsUnique();
        builder.HasIndex(x => new { x.CaseId, x.SubmittedAt });

        builder.Property(x => x.Phase).HasConversion<int>().IsRequired();
        builder.Property(x => x.RevisionNumber).IsRequired();
        builder.Property(x => x.SubmittedByUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SubmittedAt).IsRequired();

        builder.Property(x => x.ReviewedByUserId).HasMaxLength(64);
        builder.Property(x => x.ReviewResult).HasConversion<int>().IsRequired();
        builder.Property(x => x.ReviewedAt);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}

