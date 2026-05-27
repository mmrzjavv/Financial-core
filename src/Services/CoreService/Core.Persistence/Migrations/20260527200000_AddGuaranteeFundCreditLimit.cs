using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuaranteeFundCreditLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guarantee_fund_credit_limit",
                schema: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditLimitWithCheck = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiresAt = table.Column<DateOnly>(type: "date", nullable: false),
                    LastSetByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantee_fund_credit_limit", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "Cases",
                table: "guarantee_fund_credit_limit",
                columns: new[] { "Id", "CreditLimitWithCheck", "PeriodStart", "ExpiresAt", "LastSetByUserId", "CreatedAt", "UpdatedAt" },
                values: new object[] { Guid.Parse("00000000-0000-0000-0000-000000000001"), 0m, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), "system", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guarantee_fund_credit_limit",
                schema: "Cases");
        }
    }
}
