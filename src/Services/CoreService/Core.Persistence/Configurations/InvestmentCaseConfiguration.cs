using BuildingBlocks.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Services.CoreService.Core.Domain.Entities;
using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Persistence.Configurations;

public sealed class InvestmentCaseConfiguration : IEntityTypeConfiguration<InvestmentCase>
{
    public void Configure(EntityTypeBuilder<InvestmentCase> builder)
    {
        builder.ToTable("investment_cases", DbSchemas.Cases);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CaseNumber).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.CaseNumber).IsUnique();

        builder.Property(x => x.ApplicantUserId).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.ApplicantUserId);

        builder.Property(x => x.ApplicantType).HasConversion<int>().IsRequired();
        builder.Property(x => x.CurrentPhase).HasConversion<int>().IsRequired();
        builder.Property(x => x.CurrentStatus).HasConversion<int>().IsRequired();

        builder.Property(x => x.WorkflowInstanceId).HasMaxLength(128);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.CompletedAt);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .HasColumnName("xmin");

        builder.OwnsOne(x => x.Company, owned =>
        {
            owned.Property(x => x.Name).HasMaxLength(256).HasColumnName("company_name");
            owned.Property(x => x.EconomicCode).HasMaxLength(32).HasColumnName("company_economic_code");
            owned.Property(x => x.RegistrationNumber).HasMaxLength(64).HasColumnName("company_registration_number");
            owned.Property(x => x.NationalId).HasMaxLength(64).HasColumnName("company_national_id");
            owned.Property(x => x.PhoneNumber).HasMaxLength(32).HasColumnName("company_phone_number");
            owned.Property(x => x.Address).HasMaxLength(512).HasColumnName("company_address");
            owned.Property(x => x.City).HasMaxLength(128).HasColumnName("company_city");
            owned.Property(x => x.Province).HasMaxLength(128).HasColumnName("company_province");
            owned.Property(x => x.PostalCode).HasMaxLength(32).HasColumnName("company_postal_code");
        });
        builder.Navigation(x => x.Company).IsRequired(false);

        builder.HasOne(x => x.DataEntry1)
            .WithOne(x => x.Case)
            .HasForeignKey<InvestmentCaseDataEntry1>(x => x.CaseId);

        builder.HasOne(x => x.DataEntry2)
            .WithOne(x => x.Case)
            .HasForeignKey<InvestmentCaseDataEntry2>(x => x.CaseId);

        builder.HasOne(x => x.FinancialWorksheet)
            .WithOne(x => x.Case)
            .HasForeignKey<FinancialWorksheet>(x => x.CaseId);

        builder.HasMany(x => x.Documents).WithOne(x => x.Case).HasForeignKey(x => x.CaseId);
        builder.HasMany(x => x.Comments).WithOne(x => x.Case).HasForeignKey(x => x.CaseId);
        builder.HasMany(x => x.Revisions).WithOne(x => x.Case).HasForeignKey(x => x.CaseId);
        builder.HasMany(x => x.Evaluations).WithOne(x => x.Case).HasForeignKey(x => x.CaseId);
        builder.HasMany(x => x.Valuations).WithOne(x => x.Case).HasForeignKey(x => x.CaseId);
        builder.HasMany(x => x.Payments).WithOne(x => x.Case).HasForeignKey(x => x.CaseId);
        builder.HasMany(x => x.WorkflowHistory).WithOne(x => x.Case).HasForeignKey(x => x.CaseId);
    }
}
