using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RedesignDataEntry2Form : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompetitiveAdvantage",
                schema: "Cases",
                table: "case_data_entry_2");

            migrationBuilder.DropColumn(
                name: "FinancialProjection",
                schema: "Cases",
                table: "case_data_entry_2");

            migrationBuilder.DropColumn(
                name: "GoToMarketStrategy",
                schema: "Cases",
                table: "case_data_entry_2");

            migrationBuilder.DropColumn(
                name: "MarketAnalysis",
                schema: "Cases",
                table: "case_data_entry_2");

            migrationBuilder.DropColumn(
                name: "Risks",
                schema: "Cases",
                table: "case_data_entry_2");

            migrationBuilder.RenameColumn(
                name: "RevenueModel",
                schema: "Cases",
                table: "case_data_entry_2",
                newName: "InvestmentAttractionBasis");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InvestmentAttractionBasis",
                schema: "Cases",
                table: "case_data_entry_2",
                newName: "RevenueModel");

            migrationBuilder.AddColumn<string>(
                name: "CompetitiveAdvantage",
                schema: "Cases",
                table: "case_data_entry_2",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FinancialProjection",
                schema: "Cases",
                table: "case_data_entry_2",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GoToMarketStrategy",
                schema: "Cases",
                table: "case_data_entry_2",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketAnalysis",
                schema: "Cases",
                table: "case_data_entry_2",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Risks",
                schema: "Cases",
                table: "case_data_entry_2",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);
        }
    }
}
