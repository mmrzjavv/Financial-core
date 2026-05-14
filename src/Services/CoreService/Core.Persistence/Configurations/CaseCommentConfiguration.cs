using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Services.CoreService.Core.Domain.Entities;

namespace Services.CoreService.Core.Persistence.Configurations;

public sealed class CaseCommentConfiguration : IEntityTypeConfiguration<CaseComment>
{
    public void Configure(EntityTypeBuilder<CaseComment> builder)
    {
        builder.ToTable("case_comments", DbSchemas.Cases);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.CaseId, x.Phase, x.CreatedAt });

        builder.Property(x => x.SenderUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SenderRole).HasMaxLength(64);
        builder.Property(x => x.Message).HasMaxLength(8000).IsRequired();

        builder.Property(x => x.Phase).HasConversion<int>().IsRequired();
        builder.Property(x => x.IsRevisionRequest).IsRequired();
        builder.Property(x => x.IsInternal).IsRequired();

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}

