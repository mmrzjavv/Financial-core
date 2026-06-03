using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class CaseDocumentConfiguration : IEntityTypeConfiguration<CaseDocument>
{
    public void Configure(EntityTypeBuilder<CaseDocument> builder)
    {
        builder.ToTable("case_documents", DbSchemas.Investment);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.CaseId, x.DocumentType, x.Version });
        builder.HasIndex(x => x.S3Key).IsUnique();

        builder.Property(x => x.S3Key).HasMaxLength(512).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(512).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.FileSize).IsRequired();
        builder.Property(x => x.Version).IsRequired();
        builder.Property(x => x.DocumentType).HasConversion<int>().IsRequired();
        builder.Property(x => x.UploadedByUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UploadedAt).IsRequired();

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}

