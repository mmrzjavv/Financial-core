using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class DataEntry2Configuration : IEntityTypeConfiguration<InvestmentCaseDataEntry2>
{
    public void Configure(EntityTypeBuilder<InvestmentCaseDataEntry2> builder)
    {
        builder.ToTable("case_data_entry_2", DbSchemas.Investment);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.CaseId).IsUnique();

        builder.Property(x => x.InvestmentAttractionBasis).HasMaxLength(8000).IsRequired();

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}
