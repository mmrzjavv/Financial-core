using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitInvestmentAndGuaranteeSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Investment");

            migrationBuilder.EnsureSchema(
                name: "Guarantee");

            migrationBuilder.RenameTable(
                name: "payment_records",
                schema: "Cases",
                newName: "payment_records",
                newSchema: "Investment");

            migrationBuilder.RenameTable(
                name: "investment_cases",
                schema: "Cases",
                newName: "investment_cases",
                newSchema: "Investment");

            migrationBuilder.RenameTable(
                name: "guarantee_renewal_cases",
                schema: "Cases",
                newName: "guarantee_renewal_cases",
                newSchema: "Guarantee");

            migrationBuilder.RenameTable(
                name: "guarantee_fund_credit_limit",
                schema: "Cases",
                newName: "guarantee_fund_credit_limit",
                newSchema: "Guarantee");

            migrationBuilder.RenameTable(
                name: "guarantee_cases",
                schema: "Cases",
                newName: "guarantee_cases",
                newSchema: "Guarantee");

            migrationBuilder.RenameTable(
                name: "guarantee_case_workflow_history",
                schema: "Cases",
                newName: "guarantee_case_workflow_history",
                newSchema: "Guarantee");

            migrationBuilder.RenameTable(
                name: "guarantee_case_documents",
                schema: "Cases",
                newName: "guarantee_case_documents",
                newSchema: "Guarantee");

            migrationBuilder.RenameTable(
                name: "guarantee_case_comments",
                schema: "Cases",
                newName: "guarantee_case_comments",
                newSchema: "Guarantee");

            migrationBuilder.RenameTable(
                name: "guarantee_case_applications",
                schema: "Cases",
                newName: "guarantee_case_applications",
                newSchema: "Guarantee");

            migrationBuilder.RenameTable(
                name: "guarantee_approval_forms",
                schema: "Cases",
                newName: "guarantee_approval_forms",
                newSchema: "Guarantee");

            migrationBuilder.RenameTable(
                name: "guarantee_applicant_credit_profiles",
                schema: "Cases",
                newName: "guarantee_applicant_credit_profiles",
                newSchema: "Guarantee");

            migrationBuilder.RenameTable(
                name: "financial_worksheets",
                schema: "Cases",
                newName: "financial_worksheets",
                newSchema: "Investment");

            migrationBuilder.RenameTable(
                name: "case_workflow_history",
                schema: "Cases",
                newName: "case_workflow_history",
                newSchema: "Investment");

            migrationBuilder.RenameTable(
                name: "case_valuations",
                schema: "Cases",
                newName: "case_valuations",
                newSchema: "Investment");

            migrationBuilder.RenameTable(
                name: "case_revisions",
                schema: "Cases",
                newName: "case_revisions",
                newSchema: "Investment");

            migrationBuilder.RenameTable(
                name: "case_evaluations",
                schema: "Cases",
                newName: "case_evaluations",
                newSchema: "Investment");

            migrationBuilder.RenameTable(
                name: "case_evaluation_items",
                schema: "Cases",
                newName: "case_evaluation_items",
                newSchema: "Investment");

            migrationBuilder.RenameTable(
                name: "case_documents",
                schema: "Cases",
                newName: "case_documents",
                newSchema: "Investment");

            migrationBuilder.RenameTable(
                name: "case_data_entry_2",
                schema: "Cases",
                newName: "case_data_entry_2",
                newSchema: "Investment");

            migrationBuilder.RenameTable(
                name: "case_data_entry_1",
                schema: "Cases",
                newName: "case_data_entry_1",
                newSchema: "Investment");

            migrationBuilder.RenameTable(
                name: "case_comments",
                schema: "Cases",
                newName: "case_comments",
                newSchema: "Investment");

            migrationBuilder.RenameTable(
                name: "case_comment_attachments",
                schema: "Cases",
                newName: "case_comment_attachments",
                newSchema: "Investment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Cases");

            migrationBuilder.RenameTable(
                name: "payment_records",
                schema: "Investment",
                newName: "payment_records",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "investment_cases",
                schema: "Investment",
                newName: "investment_cases",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "guarantee_renewal_cases",
                schema: "Guarantee",
                newName: "guarantee_renewal_cases",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "guarantee_fund_credit_limit",
                schema: "Guarantee",
                newName: "guarantee_fund_credit_limit",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "guarantee_cases",
                schema: "Guarantee",
                newName: "guarantee_cases",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "guarantee_case_workflow_history",
                schema: "Guarantee",
                newName: "guarantee_case_workflow_history",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "guarantee_case_documents",
                schema: "Guarantee",
                newName: "guarantee_case_documents",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "guarantee_case_comments",
                schema: "Guarantee",
                newName: "guarantee_case_comments",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "guarantee_case_applications",
                schema: "Guarantee",
                newName: "guarantee_case_applications",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "guarantee_approval_forms",
                schema: "Guarantee",
                newName: "guarantee_approval_forms",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "guarantee_applicant_credit_profiles",
                schema: "Guarantee",
                newName: "guarantee_applicant_credit_profiles",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "financial_worksheets",
                schema: "Investment",
                newName: "financial_worksheets",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_workflow_history",
                schema: "Investment",
                newName: "case_workflow_history",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_valuations",
                schema: "Investment",
                newName: "case_valuations",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_revisions",
                schema: "Investment",
                newName: "case_revisions",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_evaluations",
                schema: "Investment",
                newName: "case_evaluations",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_evaluation_items",
                schema: "Investment",
                newName: "case_evaluation_items",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_documents",
                schema: "Investment",
                newName: "case_documents",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_data_entry_2",
                schema: "Investment",
                newName: "case_data_entry_2",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_data_entry_1",
                schema: "Investment",
                newName: "case_data_entry_1",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_comments",
                schema: "Investment",
                newName: "case_comments",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_comment_attachments",
                schema: "Investment",
                newName: "case_comment_attachments",
                newSchema: "Cases");
        }
    }
}
