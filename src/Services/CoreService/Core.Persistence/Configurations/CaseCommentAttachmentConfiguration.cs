using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class CaseCommentAttachmentConfiguration : IEntityTypeConfiguration<CaseCommentAttachment>
{
    public void Configure(EntityTypeBuilder<CaseCommentAttachment> builder)
    {
        builder.ToTable("case_comment_attachments", DbSchemas.Cases);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.S3Key).IsRequired();
        builder.Property(x => x.FileName).IsRequired();

        builder.HasIndex(x => x.CommentId);
    }
}
