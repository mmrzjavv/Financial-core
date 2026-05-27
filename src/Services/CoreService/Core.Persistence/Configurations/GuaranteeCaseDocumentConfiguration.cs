using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class GuaranteeCaseDocumentConfiguration : IEntityTypeConfiguration<GuaranteeCaseDocument>
{
    public void Configure(EntityTypeBuilder<GuaranteeCaseDocument> builder)
    {
        builder.ToTable("guarantee_case_documents", DbSchemas.Cases);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CaseId, x.DocumentType, x.Version });

        builder.Property(x => x.S3Key).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(512).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.UploadedByUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DocumentType).HasConversion<int>().IsRequired();

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
    }
}
