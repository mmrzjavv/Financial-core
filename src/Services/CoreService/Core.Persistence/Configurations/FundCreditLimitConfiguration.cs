using Core.Domain.Entities.Fund;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class FundCreditLimitConfiguration : IEntityTypeConfiguration<FundCreditLimit>
{
    public void Configure(EntityTypeBuilder<FundCreditLimit> builder)
    {
        builder.ToTable("fund_credit_limits", DbSchemas.Fund);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ModuleType).HasConversion<int>();
        builder.Property(x => x.CreditLimitWithCheck).HasPrecision(18, 2);
        builder.Property(x => x.PeriodStart).HasColumnType("date");
        builder.Property(x => x.ExpiresAt).HasColumnType("date");
        builder.Property(x => x.LastSetByUserId).HasMaxLength(128);
        builder.HasIndex(x => new { x.ModuleType, x.PeriodStart, x.ExpiresAt });
    }
}
