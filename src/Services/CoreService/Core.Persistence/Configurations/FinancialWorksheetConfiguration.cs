using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class FinancialWorksheetConfiguration : IEntityTypeConfiguration<InvestmentCaseFinancialWorksheet>
{
    public void Configure(EntityTypeBuilder<InvestmentCaseFinancialWorksheet> builder)
    {
        builder.ToTable("financial_worksheets", DbSchemas.Investment);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.CaseId).IsUnique();
        builder.HasIndex(x => x.ApprovedAmount);

        builder.Property(x => x.BankName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Iban).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ApprovedAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.PaymentSchedule).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}

