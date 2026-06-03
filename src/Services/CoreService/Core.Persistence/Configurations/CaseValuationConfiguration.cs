using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class CaseValuationConfiguration : IEntityTypeConfiguration<CaseValuation>
{
    public void Configure(EntityTypeBuilder<CaseValuation> builder)
    {
        builder.ToTable("case_valuations", DbSchemas.Investment);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.CaseId, x.Type, x.CreatedAt });

        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.CreatedByUserId).HasMaxLength(64).IsRequired();

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}

