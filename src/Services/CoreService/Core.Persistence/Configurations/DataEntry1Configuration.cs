using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class DataEntry1Configuration : IEntityTypeConfiguration<InvestmentCaseDataEntry1>
{
    public void Configure(EntityTypeBuilder<InvestmentCaseDataEntry1> builder)
    {
        builder.ToTable("case_data_entry_1", DbSchemas.Investment);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.CaseId).IsUnique();

        builder.Property(x => x.RepresentativeFullName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.BusinessStage).HasConversion<int>().IsRequired();
        builder.Property(x => x.ContactEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.RequestedAmount).HasPrecision(18, 2).IsRequired();

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}
