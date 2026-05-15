using BuildingBlocks.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Services.CoreService.Core.Domain.Entities;
using Services.CoreService.Core.Domain.Identity.Entities;

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

        builder.Property(x => x.CompanyId);
        builder.HasIndex(x => x.CompanyId);

        builder.HasOne(x => x.ApplicantCompany)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.CompletedAt);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt);

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
