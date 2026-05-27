using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuaranteeFundCreditLimitPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodStart",
                schema: "Cases",
                table: "guarantee_fund_credit_limit",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(2026, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpiresAt",
                schema: "Cases",
                table: "guarantee_fund_credit_limit",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(2026, 12, 31));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PeriodStart",
                schema: "Cases",
                table: "guarantee_fund_credit_limit");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "Cases",
                table: "guarantee_fund_credit_limit");
        }
    }
}
