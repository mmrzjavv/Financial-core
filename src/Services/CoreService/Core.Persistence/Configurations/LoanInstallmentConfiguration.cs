using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class LoanInstallmentConfiguration : IEntityTypeConfiguration<LoanInstallment>
{
    public void Configure(EntityTypeBuilder<LoanInstallment> builder)
    {
        builder.ToTable("loan_installments", DbSchemas.Loan);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CaseId, x.RowNumber }).IsUnique();
        builder.HasIndex(x => new { x.CaseId, x.IsPaid });
        builder.HasIndex(x => new { x.InstallmentDate, x.IsPaid, x.IsGracePeriod });

        builder.Property(x => x.PrincipalAmount).HasPrecision(18, 2);
        builder.Property(x => x.ProfitAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.FundShareOfPrincipal).HasPrecision(18, 2);
        builder.Property(x => x.FundShareOfProfit).HasPrecision(18, 2);
        builder.Property(x => x.FundShareOfTotal).HasPrecision(18, 2);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}
