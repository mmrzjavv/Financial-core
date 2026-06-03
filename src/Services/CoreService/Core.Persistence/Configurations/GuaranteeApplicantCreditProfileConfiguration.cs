using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class GuaranteeApplicantCreditProfileConfiguration : IEntityTypeConfiguration<GuaranteeApplicantCreditProfile>
{
    public void Configure(EntityTypeBuilder<GuaranteeApplicantCreditProfile> builder)
    {
        builder.ToTable("guarantee_applicant_credit_profiles", DbSchemas.Guarantee);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApplicantUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreditLimitWithCheck).HasPrecision(18, 2);
        builder.Property(x => x.LastSetByUserId).HasMaxLength(64);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => x.ApplicantUserId)
            .IsUnique()
            .HasFilter("\"CompanyId\" IS NULL");

        builder.HasIndex(x => x.CompanyId)
            .IsUnique()
            .HasFilter("\"CompanyId\" IS NOT NULL");
    }
}
