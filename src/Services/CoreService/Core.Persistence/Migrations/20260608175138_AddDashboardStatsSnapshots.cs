using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardStatsSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Analytics");

            migrationBuilder.CreateTable(
                name: "dashboard_stats_snapshots",
                schema: "Analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SnapshotType = table.Column<int>(type: "integer", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    ComputedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_stats_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_stats_snapshots_ComputedAtUtc",
                schema: "Analytics",
                table: "dashboard_stats_snapshots",
                column: "ComputedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_stats_snapshots_SnapshotKey",
                schema: "Analytics",
                table: "dashboard_stats_snapshots",
                column: "SnapshotKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_stats_snapshots_SnapshotType",
                schema: "Analytics",
                table: "dashboard_stats_snapshots",
                column: "SnapshotType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dashboard_stats_snapshots",
                schema: "Analytics");
        }
    }
}
