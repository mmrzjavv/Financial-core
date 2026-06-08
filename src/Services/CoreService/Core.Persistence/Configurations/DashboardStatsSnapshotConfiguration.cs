using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Persistence.Configurations;

public sealed class DashboardStatsSnapshotConfiguration : IEntityTypeConfiguration<DashboardStatsSnapshot>
{
    public void Configure(EntityTypeBuilder<DashboardStatsSnapshot> builder)
    {
        builder.ToTable("dashboard_stats_snapshots", DbSchemas.Analytics);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SnapshotKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ComputedAtUtc).HasDefaultValueSql("timezone('utc', now())");

        builder.HasIndex(x => x.SnapshotKey).IsUnique();
        builder.HasIndex(x => x.SnapshotType);
        builder.HasIndex(x => x.ComputedAtUtc);
    }
}
