using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class GuaranteeApprovalFormConfiguration : IEntityTypeConfiguration<GuaranteeApprovalForm>
{
    public void Configure(EntityTypeBuilder<GuaranteeApprovalForm> builder)
    {
        builder.ToTable("guarantee_approval_forms", DbSchemas.Guarantee);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CaseId).IsUnique();

        builder.Property(x => x.GuaranteeAmountInWords).HasMaxLength(1024);
        builder.Property(x => x.ContractSubject).HasMaxLength(2000);
        builder.Property(x => x.Beneficiary).HasMaxLength(512);
        builder.Property(x => x.CollateralDescription).HasMaxLength(4000);
        builder.Property(x => x.GuarantorsDescription).HasMaxLength(4000);
        builder.Property(x => x.OtherNotes).HasMaxLength(4000);

        builder.Property(x => x.GuaranteeType).HasConversion<int?>();

        builder.Property(x => x.CreditLimitWithCheck).HasPrecision(18, 2);
        builder.Property(x => x.FundIssuedGuaranteesTotal).HasPrecision(18, 2);
        builder.Property(x => x.ActiveCommitments).HasPrecision(18, 2);
        builder.Property(x => x.RemainingCredit).HasPrecision(18, 2);
        builder.Property(x => x.GuaranteeAmount).HasPrecision(18, 2);
        builder.Property(x => x.DepositRatePercent).HasPrecision(8, 4);
        builder.Property(x => x.DepositAmount).HasPrecision(18, 2);
        builder.Property(x => x.AnnualCommissionRatePercent).HasPrecision(8, 4);
        builder.Property(x => x.CommissionAmount).HasPrecision(18, 2);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}
