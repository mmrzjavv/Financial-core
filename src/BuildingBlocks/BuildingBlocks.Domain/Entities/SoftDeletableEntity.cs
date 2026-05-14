namespace BuildingBlocks.Domain.Entities;

public abstract class SoftDeletableEntity : AuditableEntity
{
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    public string? DeletedBy { get; protected set; }

    public void SoftDelete(DateTimeOffset deletedAt, string? deletedBy)
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
    }
}

