using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Services.CoreService.Core.Domain.Entities;

namespace Services.CoreService.Core.Persistence.Configurations;

public sealed class DataEntry1Configuration : IEntityTypeConfiguration<InvestmentCaseDataEntry1>
{
    public void Configure(EntityTypeBuilder<InvestmentCaseDataEntry1> builder)
    {
        builder.ToTable("case_data_entry_1", DbSchemas.Cases);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.CaseId).IsUnique();

        builder.Property(x => x.StartupTitle).HasMaxLength(256).IsRequired();
        builder.Property(x => x.BusinessDescription).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.RequestedAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.TeamSize).IsRequired();
        builder.Property(x => x.Website).HasMaxLength(512);

        builder.Property(x => x.Country).HasMaxLength(128);
        builder.Property(x => x.City).HasMaxLength(128);
        builder.Property(x => x.Industry).HasMaxLength(128);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}

