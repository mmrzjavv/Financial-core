using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuaranteeCaseFilterIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_guarantee_cases_ApplicantUserId_CreatedAt",
                schema: "Guarantee",
                table: "guarantee_cases",
                columns: new[] { "ApplicantUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_cases_CompanyId_CreatedAt",
                schema: "Guarantee",
                table: "guarantee_cases",
                columns: new[] { "CompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_cases_CurrentPhase_CreatedAt",
                schema: "Guarantee",
                table: "guarantee_cases",
                columns: new[] { "CurrentPhase", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_cases_CurrentStatus_CreatedAt",
                schema: "Guarantee",
                table: "guarantee_cases",
                columns: new[] { "CurrentStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_case_applications_BeneficiaryNationalId",
                schema: "Guarantee",
                table: "guarantee_case_applications",
                column: "BeneficiaryNationalId");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_case_applications_GuaranteeType",
                schema: "Guarantee",
                table: "guarantee_case_applications",
                column: "GuaranteeType");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_case_applications_RequestedGuaranteeAmount",
                schema: "Guarantee",
                table: "guarantee_case_applications",
                column: "RequestedGuaranteeAmount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_guarantee_cases_ApplicantUserId_CreatedAt",
                schema: "Guarantee",
                table: "guarantee_cases");

            migrationBuilder.DropIndex(
                name: "IX_guarantee_cases_CompanyId_CreatedAt",
                schema: "Guarantee",
                table: "guarantee_cases");

            migrationBuilder.DropIndex(
                name: "IX_guarantee_cases_CurrentPhase_CreatedAt",
                schema: "Guarantee",
                table: "guarantee_cases");

            migrationBuilder.DropIndex(
                name: "IX_guarantee_cases_CurrentStatus_CreatedAt",
                schema: "Guarantee",
                table: "guarantee_cases");

            migrationBuilder.DropIndex(
                name: "IX_guarantee_case_applications_BeneficiaryNationalId",
                schema: "Guarantee",
                table: "guarantee_case_applications");

            migrationBuilder.DropIndex(
                name: "IX_guarantee_case_applications_GuaranteeType",
                schema: "Guarantee",
                table: "guarantee_case_applications");

            migrationBuilder.DropIndex(
                name: "IX_guarantee_case_applications_RequestedGuaranteeAmount",
                schema: "Guarantee",
                table: "guarantee_case_applications");
        }
    }
}
