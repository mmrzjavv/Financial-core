using Core.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations.Identity;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", DbSchemas.Identity);
        builder.HasKey(e => e.Id);
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.TokenHash).IsRequired().HasMaxLength(128);
        builder.HasIndex(e => new { e.UserId, e.SessionId });
        builder.HasIndex(e => e.TokenHash).IsUnique();
    }
}
