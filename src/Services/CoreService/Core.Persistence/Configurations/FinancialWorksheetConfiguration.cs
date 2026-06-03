using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class FinancialWorksheetConfiguration : IEntityTypeConfiguration<FinancialWorksheet>
{
    public void Configure(EntityTypeBuilder<FinancialWorksheet> builder)
    {
        builder.ToTable("financial_worksheets", DbSchemas.Investment);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.CaseId).IsUnique();

        builder.Property(x => x.BankName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Iban).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ApprovedAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.PaymentSchedule).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}

