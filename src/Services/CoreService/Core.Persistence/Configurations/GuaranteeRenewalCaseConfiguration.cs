using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class GuaranteeRenewalCaseConfiguration : IEntityTypeConfiguration<GuaranteeRenewalCase>
{
    public void Configure(EntityTypeBuilder<GuaranteeRenewalCase> builder)
    {
        builder.ToTable("guarantee_renewal_cases", DbSchemas.Guarantee);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CaseNumber).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.CaseNumber).IsUnique();

        builder.Property(x => x.ApplicantUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CurrentStatus).HasConversion<int>().IsRequired();
        builder.Property(x => x.RenewalKind).HasConversion<int>().IsRequired();
        builder.Property(x => x.WorkflowInstanceId).HasMaxLength(128);

        builder.Property(x => x.RequestedAmount).HasPrecision(18, 2);

        builder.HasOne(x => x.ParentGuaranteeCase)
            .WithMany()
            .HasForeignKey(x => x.ParentGuaranteeCaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
    }
}
