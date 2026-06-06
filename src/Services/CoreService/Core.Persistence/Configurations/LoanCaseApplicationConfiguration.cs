using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class LoanCaseApplicationConfiguration : IEntityTypeConfiguration<LoanCaseApplication>
{
    public void Configure(EntityTypeBuilder<LoanCaseApplication> builder)
    {
        builder.ToTable("loan_case_applications", DbSchemas.Loan);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CaseId).IsUnique();

        builder.Property(x => x.RequestedAmount).HasPrecision(18, 2);
        builder.Property(x => x.RequestedAmountInWords).HasMaxLength(1024);
        builder.Property(x => x.FacilitySubject).HasMaxLength(2000);
        builder.Property(x => x.OfferedGuarantees).HasMaxLength(4000);
        builder.Property(x => x.ApplicantCategoryOther).HasMaxLength(512);
        builder.Property(x => x.RepresentativePosition).HasMaxLength(256);
        builder.Property(x => x.ApplicantCategory).HasConversion<int>();

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}
