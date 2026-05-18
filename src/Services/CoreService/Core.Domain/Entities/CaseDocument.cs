using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities;

public sealed class CaseDocument : Entity<Guid>, IAuditableEntity, ISoftDelete
{
    private CaseDocument()
    {
        S3Key = default!;
        FileName = default!;
        MimeType = default!;
        UploadedByUserId = default!;
    }

    public CaseDocument(
        Guid caseId,
        string s3Key,
        string fileName,
        string mimeType,
        long fileSize,
        int version,
        DocumentType documentType,
        string uploadedByUserId,
        DateTimeOffset uploadedAt)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        S3Key = s3Key;
        FileName = fileName;
        MimeType = mimeType;
        FileSize = fileSize;
        Version = version;
        DocumentType = documentType;
        UploadedByUserId = uploadedByUserId;
        UploadedAt = uploadedAt;
        CreatedAt = uploadedAt;
    }

    public Guid CaseId { get; private set; }
    public InvestmentCase Case { get; private set; } = default!;

    public string S3Key { get; private set; }
    public string FileName { get; private set; }
    public string MimeType { get; private set; }
    public long FileSize { get; private set; }
    public int Version { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public string UploadedByUserId { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

