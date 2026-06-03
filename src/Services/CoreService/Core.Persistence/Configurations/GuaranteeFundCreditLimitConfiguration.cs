using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class GuaranteeFundCreditLimitConfiguration : IEntityTypeConfiguration<GuaranteeFundCreditLimit>
{
    public void Configure(EntityTypeBuilder<GuaranteeFundCreditLimit> builder)
    {
        builder.ToTable("guarantee_fund_credit_limit", DbSchemas.Guarantee);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreditLimitWithCheck).HasPrecision(18, 2);
        builder.Property(x => x.PeriodStart).HasColumnType("date");
        builder.Property(x => x.ExpiresAt).HasColumnType("date");
        builder.Property(x => x.LastSetByUserId).HasMaxLength(64);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}
