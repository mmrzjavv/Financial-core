using BuildingBlocks.Domain.Entities;

namespace Core.Domain.Entities;

public sealed class CaseCommentAttachment : Entity<Guid>
{
    private CaseCommentAttachment() { }

    public CaseCommentAttachment(Guid commentId, string s3Key, string fileName)
    {
        Id = Guid.NewGuid();
        CommentId = commentId;
        S3Key = s3Key;
        FileName = fileName;
    }

    public Guid CommentId { get; private set; }
    public string S3Key { get; private set; } = default!;
    public string FileName { get; private set; } = default!;
}