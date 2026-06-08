using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class CaseCommentAttachmentConfiguration : IEntityTypeConfiguration<InvestmentCaseCommentAttachment>
{
    public void Configure(EntityTypeBuilder<InvestmentCaseCommentAttachment> builder)
    {
        builder.ToTable("case_comment_attachments", DbSchemas.Investment);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.S3Key).IsRequired();
        builder.Property(x => x.FileName).IsRequired();

        builder.HasIndex(x => x.CommentId);
    }
}
