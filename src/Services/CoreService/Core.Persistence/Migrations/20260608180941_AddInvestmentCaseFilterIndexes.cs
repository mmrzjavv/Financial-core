using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentCaseFilterIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_investment_cases_ApplicantType_CreatedAt",
                schema: "Investment",
                table: "investment_cases",
                columns: new[] { "ApplicantType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_investment_cases_ApplicantUserId_CreatedAt",
                schema: "Investment",
                table: "investment_cases",
                columns: new[] { "ApplicantUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_investment_cases_CompanyId_CreatedAt",
                schema: "Investment",
                table: "investment_cases",
                columns: new[] { "CompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_investment_cases_CurrentPhase_CreatedAt",
                schema: "Investment",
                table: "investment_cases",
                columns: new[] { "CurrentPhase", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_investment_cases_CurrentStatus_CreatedAt",
                schema: "Investment",
                table: "investment_cases",
                columns: new[] { "CurrentStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_worksheets_ApprovedAmount",
                schema: "Investment",
                table: "financial_worksheets",
                column: "ApprovedAmount");

            migrationBuilder.CreateIndex(
                name: "IX_case_data_entry_1_BusinessStage",
                schema: "Investment",
                table: "case_data_entry_1",
                column: "BusinessStage");

            migrationBuilder.CreateIndex(
                name: "IX_case_data_entry_1_ContactEmail",
                schema: "Investment",
                table: "case_data_entry_1",
                column: "ContactEmail");

            migrationBuilder.CreateIndex(
                name: "IX_case_data_entry_1_RequestedAmount",
                schema: "Investment",
                table: "case_data_entry_1",
                column: "RequestedAmount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_investment_cases_ApplicantType_CreatedAt",
                schema: "Investment",
                table: "investment_cases");

            migrationBuilder.DropIndex(
                name: "IX_investment_cases_ApplicantUserId_CreatedAt",
                schema: "Investment",
                table: "investment_cases");

            migrationBuilder.DropIndex(
                name: "IX_investment_cases_CompanyId_CreatedAt",
                schema: "Investment",
                table: "investment_cases");

            migrationBuilder.DropIndex(
                name: "IX_investment_cases_CurrentPhase_CreatedAt",
                schema: "Investment",
                table: "investment_cases");

            migrationBuilder.DropIndex(
                name: "IX_investment_cases_CurrentStatus_CreatedAt",
                schema: "Investment",
                table: "investment_cases");

            migrationBuilder.DropIndex(
                name: "IX_financial_worksheets_ApprovedAmount",
                schema: "Investment",
                table: "financial_worksheets");

            migrationBuilder.DropIndex(
                name: "IX_case_data_entry_1_BusinessStage",
                schema: "Investment",
                table: "case_data_entry_1");

            migrationBuilder.DropIndex(
                name: "IX_case_data_entry_1_ContactEmail",
                schema: "Investment",
                table: "case_data_entry_1");

            migrationBuilder.DropIndex(
                name: "IX_case_data_entry_1_RequestedAmount",
                schema: "Investment",
                table: "case_data_entry_1");
        }
    }
}
