using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Services.CoreService.Core.Domain.Entities;

namespace Services.CoreService.Core.Persistence.Configurations;

public sealed class DataEntry2Configuration : IEntityTypeConfiguration<InvestmentCaseDataEntry2>
{
    public void Configure(EntityTypeBuilder<InvestmentCaseDataEntry2> builder)
    {
        builder.ToTable("case_data_entry_2", DbSchemas.Cases);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.CaseId).IsUnique();

        builder.Property(x => x.MarketAnalysis).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.RevenueModel).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.CompetitiveAdvantage).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.FinancialProjection).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.Risks).HasMaxLength(8000);
        builder.Property(x => x.GoToMarketStrategy).HasMaxLength(8000);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt);
    }
}

