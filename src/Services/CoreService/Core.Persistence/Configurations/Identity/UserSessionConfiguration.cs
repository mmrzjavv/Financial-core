using Core.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations.Identity;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions", DbSchemas.Identity);
        builder.HasKey(e => e.Id);
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.SessionId).IsRequired();
        builder.HasIndex(e => e.SessionId).IsUnique();
        builder.HasIndex(e => new { e.UserId, e.RevokedAt });
    }
}
