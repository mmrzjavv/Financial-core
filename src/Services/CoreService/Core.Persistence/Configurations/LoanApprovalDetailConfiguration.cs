using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class LoanApprovalDetailConfiguration : IEntityTypeConfiguration<LoanApprovalDetail>
{
    public void Configure(EntityTypeBuilder<LoanApprovalDetail> builder)
    {
        builder.ToTable("loan_approval_details", DbSchemas.Loan);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CaseId).IsUnique();

        builder.Property(x => x.DebtToAssetRatio).HasPrecision(10, 4);
        builder.Property(x => x.CurrentRatio).HasPrecision(10, 4);
        builder.Property(x => x.ProfitabilityRatioPercent).HasPrecision(10, 4);
        builder.Property(x => x.CreditLimitWithCheck).HasPrecision(18, 2);
        builder.Property(x => x.RemainingCreditAfterGrant).HasPrecision(18, 2);
        builder.Property(x => x.ApprovedAmount).HasPrecision(18, 2);
        builder.Property(x => x.ApprovedAmountInWords).HasMaxLength(1024);
        builder.Property(x => x.AnnualProfitRatePercent).HasPrecision(10, 4);
        builder.Property(x => x.DailyPenaltyRatePercent).HasPrecision(10, 4);
        builder.Property(x => x.ExpectedTotalProfit).HasPrecision(18, 2);
        builder.Property(x => x.RepaymentCheckAmount).HasPrecision(18, 2);
        builder.Property(x => x.ContractSubject).HasMaxLength(2000);
        builder.Property(x => x.BrokerageAndRelatedContract).HasMaxLength(2000);
        builder.Property(x => x.CollateralDescription).HasMaxLength(4000);
        builder.Property(x => x.GuarantorsDescription).HasMaxLength(4000);
        builder.Property(x => x.OtherNotes).HasMaxLength(4000);
        builder.Property(x => x.FacilityType).HasConversion<int?>();

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}
