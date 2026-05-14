using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Services.CoreService.Core.Domain.Identity.Entities;

namespace Services.CoreService.Core.Persistence.Configurations.Identity;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions", "Identity");
        builder.HasKey(e => e.Id);
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.SessionId).IsRequired();
        builder.HasIndex(e => e.SessionId).IsUnique();
        builder.HasIndex(e => new { e.UserId, e.RevokedAt });
    }
}
