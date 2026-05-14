using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Domain.Entities;

public sealed class CaseComment : Entity<Guid>, IAuditableEntity
{
    private CaseComment()
    {
        SenderUserId = default!;
        Message = default!;
    }

    public CaseComment(
        Guid caseId,
        CasePhase phase,
        string senderUserId,
        string? senderRole,
        string message,
        bool isRevisionRequest,
        bool isInternal,
        Guid? parentId = null)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        Phase = phase;
        SenderUserId = senderUserId;
        SenderRole = senderRole;
        Message = message;
        IsRevisionRequest = isRevisionRequest;
        IsInternal = isInternal;
        ParentId = parentId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid CaseId { get; private set; }
    public InvestmentCase Case { get; private set; } = default!;

    public CasePhase Phase { get; private set; }
    public string SenderUserId { get; private set; }
    public string? SenderRole { get; private set; }
    public string Message { get; private set; }
    public bool IsRevisionRequest { get; private set; }
    public bool IsInternal { get; private set; }

    public Guid? ParentId { get; private set; }
    public CaseComment? Parent { get; private set; }
    public List<CaseComment> Replies { get; private set; } = [];
    public List<CaseCommentAttachment> Attachments { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void AddAttachment(string s3Key, string fileName)
    {
        Attachments.Add(new CaseCommentAttachment(Id, s3Key, fileName));
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

