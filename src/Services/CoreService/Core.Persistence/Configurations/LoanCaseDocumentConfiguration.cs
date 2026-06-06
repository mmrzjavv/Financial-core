using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class LoanCaseDocumentConfiguration : IEntityTypeConfiguration<LoanCaseDocument>
{
    public void Configure(EntityTypeBuilder<LoanCaseDocument> builder)
    {
        builder.ToTable("loan_case_documents", DbSchemas.Loan);
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
