using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Services.CoreService.Core.Domain.Entities;

namespace Services.CoreService.Core.Persistence.Configurations;

public sealed class PaymentRecordConfiguration : IEntityTypeConfiguration<PaymentRecord>
{
    public void Configure(EntityTypeBuilder<PaymentRecord> builder)
    {
        builder.ToTable("payment_records");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.CaseId, x.PaymentDate });
        builder.HasIndex(x => x.TransactionNumber).IsUnique();

        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.PaymentDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.TransactionNumber).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ReceiptS3Key).HasMaxLength(512);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(64).IsRequired();

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}

