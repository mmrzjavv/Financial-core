using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class LoanPaymentConfiguration : IEntityTypeConfiguration<LoanPayment>
{
    public void Configure(EntityTypeBuilder<LoanPayment> builder)
    {
        builder.ToTable("loan_payments", DbSchemas.Loan);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CaseId, x.StageNumber }).IsUnique();

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.TransactionNumber).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ReceiptS3Key).HasMaxLength(1024);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(64).IsRequired();

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}
