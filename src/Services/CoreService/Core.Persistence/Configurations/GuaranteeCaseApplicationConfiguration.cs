using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class GuaranteeCaseApplicationConfiguration : IEntityTypeConfiguration<GuaranteeCaseApplication>
{
    public void Configure(EntityTypeBuilder<GuaranteeCaseApplication> builder)
    {
        builder.ToTable("guarantee_case_applications", DbSchemas.Cases);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CaseId).IsUnique();

        builder.Property(x => x.ContractSubject).HasMaxLength(2000);
        builder.Property(x => x.BeneficiaryName).HasMaxLength(512);
        builder.Property(x => x.BeneficiaryNationalId).HasMaxLength(32);
        builder.Property(x => x.ApplicantCategoryOther).HasMaxLength(512);
        builder.Property(x => x.BaseContractNumber).HasMaxLength(128);
        builder.Property(x => x.BaseContractAmountInWords).HasMaxLength(1024);
        builder.Property(x => x.ExecutionProvince).HasMaxLength(128);
        builder.Property(x => x.CollateralDescription).HasMaxLength(4000);
        builder.Property(x => x.FacilitySubject).HasMaxLength(2000);

        builder.Property(x => x.GuaranteeType).HasConversion<int?>();
        builder.Property(x => x.BeneficiaryCompanyType).HasConversion<int?>();
        builder.Property(x => x.ApplicantLegalForm).HasConversion<int?>();
        builder.Property(x => x.ApplicantCategory).HasConversion<int>();

        builder.Property(x => x.BaseContractAmount).HasPrecision(18, 2);
        builder.Property(x => x.PriceAdjustmentRatePercent).HasPrecision(5, 2);
        builder.Property(x => x.RequestedGuaranteeAmount).HasPrecision(18, 2);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}
