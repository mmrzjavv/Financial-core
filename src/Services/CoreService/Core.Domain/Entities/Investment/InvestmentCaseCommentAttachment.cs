using BuildingBlocks.Domain.Entities;

namespace Core.Domain.Entities.Investment;

public sealed class InvestmentCaseCommentAttachment : Entity<Guid>
{
    private InvestmentCaseCommentAttachment() { }

    public InvestmentCaseCommentAttachment(Guid commentId, string s3Key, string fileName)
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