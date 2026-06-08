using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UnifyFundCreditLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_case_comment_attachments_case_comments_CaseCommentId",
                schema: "Investment",
                table: "case_comment_attachments");

            migrationBuilder.EnsureSchema(
                name: "Fund");

            migrationBuilder.RenameColumn(
                name: "CaseCommentId",
                schema: "Investment",
                table: "case_comment_attachments",
                newName: "InvestmentCaseCommentId");

            migrationBuilder.RenameIndex(
                name: "IX_case_comment_attachments_CaseCommentId",
                schema: "Investment",
                table: "case_comment_attachments",
                newName: "IX_case_comment_attachments_InvestmentCaseCommentId");

            migrationBuilder.CreateTable(
                name: "fund_credit_limits",
                schema: "Fund",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleType = table.Column<int>(type: "integer", nullable: false),
                    CreditLimitWithCheck = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiresAt = table.Column<DateOnly>(type: "date", nullable: false),
                    LastSetByUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fund_credit_limits", x => x.Id);
                });

            migrationBuilder.Sql("""
                INSERT INTO "Fund"."fund_credit_limits"
                    ("Id", "ModuleType", "CreditLimitWithCheck", "PeriodStart", "ExpiresAt", "LastSetByUserId", "CreatedAt", "UpdatedAt")
                SELECT
                    "Id",
                    1,
                    "CreditLimitWithCheck",
                    "PeriodStart",
                    "ExpiresAt",
                    "LastSetByUserId",
                    "CreatedAt",
                    "UpdatedAt"
                FROM "Guarantee"."guarantee_fund_credit_limit";
                """);

            migrationBuilder.DropTable(
                name: "guarantee_fund_credit_limit",
                schema: "Guarantee");

            migrationBuilder.CreateIndex(
                name: "IX_loan_installments_CaseId_IsPaid",
                schema: "Loan",
                table: "loan_installments",
                columns: new[] { "CaseId", "IsPaid" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_cases_ApplicantType_CreatedAt",
                schema: "Loan",
                table: "loan_cases",
                columns: new[] { "ApplicantType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_cases_ApplicantUserId_CreatedAt",
                schema: "Loan",
                table: "loan_cases",
                columns: new[] { "ApplicantUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_cases_CompanyId_CreatedAt",
                schema: "Loan",
                table: "loan_cases",
                columns: new[] { "CompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_cases_CurrentPhase_CreatedAt",
                schema: "Loan",
                table: "loan_cases",
                columns: new[] { "CurrentPhase", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_cases_CurrentStatus_CreatedAt",
                schema: "Loan",
                table: "loan_cases",
                columns: new[] { "CurrentStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_case_applications_ApplicantCategory",
                schema: "Loan",
                table: "loan_case_applications",
                column: "ApplicantCategory");

            migrationBuilder.CreateIndex(
                name: "IX_loan_case_applications_RequestedAmount",
                schema: "Loan",
                table: "loan_case_applications",
                column: "RequestedAmount");

            migrationBuilder.CreateIndex(
                name: "IX_loan_approval_details_ApprovedAmount",
                schema: "Loan",
                table: "loan_approval_details",
                column: "ApprovedAmount");

            migrationBuilder.CreateIndex(
                name: "IX_loan_approval_details_FacilityType",
                schema: "Loan",
                table: "loan_approval_details",
                column: "FacilityType");

            migrationBuilder.CreateIndex(
                name: "IX_loan_approval_details_IsCreditLineActive",
                schema: "Loan",
                table: "loan_approval_details",
                column: "IsCreditLineActive");

            migrationBuilder.CreateIndex(
                name: "IX_loan_approval_details_RepaymentMonths",
                schema: "Loan",
                table: "loan_approval_details",
                column: "RepaymentMonths");

            migrationBuilder.CreateIndex(
                name: "IX_fund_credit_limits_ModuleType_PeriodStart_ExpiresAt",
                schema: "Fund",
                table: "fund_credit_limits",
                columns: new[] { "ModuleType", "PeriodStart", "ExpiresAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_case_comment_attachments_case_comments_InvestmentCaseCommen~",
                schema: "Investment",
                table: "case_comment_attachments",
                column: "InvestmentCaseCommentId",
                principalSchema: "Investment",
                principalTable: "case_comments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_case_comment_attachments_case_comments_InvestmentCaseCommen~",
                schema: "Investment",
                table: "case_comment_attachments");

            migrationBuilder.DropTable(
                name: "fund_credit_limits",
                schema: "Fund");

            migrationBuilder.DropIndex(
                name: "IX_loan_installments_CaseId_IsPaid",
                schema: "Loan",
                table: "loan_installments");

            migrationBuilder.DropIndex(
                name: "IX_loan_cases_ApplicantType_CreatedAt",
                schema: "Loan",
                table: "loan_cases");

            migrationBuilder.DropIndex(
                name: "IX_loan_cases_ApplicantUserId_CreatedAt",
                schema: "Loan",
                table: "loan_cases");

            migrationBuilder.DropIndex(
                name: "IX_loan_cases_CompanyId_CreatedAt",
                schema: "Loan",
                table: "loan_cases");

            migrationBuilder.DropIndex(
                name: "IX_loan_cases_CurrentPhase_CreatedAt",
                schema: "Loan",
                table: "loan_cases");

            migrationBuilder.DropIndex(
                name: "IX_loan_cases_CurrentStatus_CreatedAt",
                schema: "Loan",
                table: "loan_cases");

            migrationBuilder.DropIndex(
                name: "IX_loan_case_applications_ApplicantCategory",
                schema: "Loan",
                table: "loan_case_applications");

            migrationBuilder.DropIndex(
                name: "IX_loan_case_applications_RequestedAmount",
                schema: "Loan",
                table: "loan_case_applications");

            migrationBuilder.DropIndex(
                name: "IX_loan_approval_details_ApprovedAmount",
                schema: "Loan",
                table: "loan_approval_details");

            migrationBuilder.DropIndex(
                name: "IX_loan_approval_details_FacilityType",
                schema: "Loan",
                table: "loan_approval_details");

            migrationBuilder.DropIndex(
                name: "IX_loan_approval_details_IsCreditLineActive",
                schema: "Loan",
                table: "loan_approval_details");

            migrationBuilder.DropIndex(
                name: "IX_loan_approval_details_RepaymentMonths",
                schema: "Loan",
                table: "loan_approval_details");

            migrationBuilder.RenameColumn(
                name: "InvestmentCaseCommentId",
                schema: "Investment",
                table: "case_comment_attachments",
                newName: "CaseCommentId");

            migrationBuilder.RenameIndex(
                name: "IX_case_comment_attachments_InvestmentCaseCommentId",
                schema: "Investment",
                table: "case_comment_attachments",
                newName: "IX_case_comment_attachments_CaseCommentId");

            migrationBuilder.CreateTable(
                name: "guarantee_fund_credit_limit",
                schema: "Guarantee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    CreditLimitWithCheck = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpiresAt = table.Column<DateOnly>(type: "date", nullable: false),
                    LastSetByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantee_fund_credit_limit", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_case_comment_attachments_case_comments_CaseCommentId",
                schema: "Investment",
                table: "case_comment_attachments",
                column: "CaseCommentId",
                principalSchema: "Investment",
                principalTable: "case_comments",
                principalColumn: "Id");
        }
    }
}
