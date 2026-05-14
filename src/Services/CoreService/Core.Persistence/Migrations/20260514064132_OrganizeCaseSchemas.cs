using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrganizeCaseSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaseCommentAttachment_case_comments_CaseCommentId",
                table: "CaseCommentAttachment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CaseCommentAttachment",
                table: "CaseCommentAttachment");

            migrationBuilder.EnsureSchema(
                name: "Cases");

            migrationBuilder.RenameTable(
                name: "payment_records",
                newName: "payment_records",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "investment_cases",
                newName: "investment_cases",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "financial_worksheets",
                newName: "financial_worksheets",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_workflow_history",
                newName: "case_workflow_history",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_valuations",
                newName: "case_valuations",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_revisions",
                newName: "case_revisions",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_evaluations",
                newName: "case_evaluations",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_evaluation_items",
                newName: "case_evaluation_items",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_documents",
                newName: "case_documents",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_data_entry_2",
                newName: "case_data_entry_2",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_data_entry_1",
                newName: "case_data_entry_1",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "case_comments",
                newName: "case_comments",
                newSchema: "Cases");

            migrationBuilder.RenameTable(
                name: "CaseCommentAttachment",
                newName: "case_comment_attachments",
                newSchema: "Cases");

            migrationBuilder.RenameIndex(
                name: "IX_CaseCommentAttachment_CaseCommentId",
                schema: "Cases",
                table: "case_comment_attachments",
                newName: "IX_case_comment_attachments_CaseCommentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_case_comment_attachments",
                schema: "Cases",
                table: "case_comment_attachments",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_case_comment_attachments_CommentId",
                schema: "Cases",
                table: "case_comment_attachments",
                column: "CommentId");

            migrationBuilder.AddForeignKey(
                name: "FK_case_comment_attachments_case_comments_CaseCommentId",
                schema: "Cases",
                table: "case_comment_attachments",
                column: "CaseCommentId",
                principalSchema: "Cases",
                principalTable: "case_comments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_case_comment_attachments_case_comments_CaseCommentId",
                schema: "Cases",
                table: "case_comment_attachments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_case_comment_attachments",
                schema: "Cases",
                table: "case_comment_attachments");

            migrationBuilder.DropIndex(
                name: "IX_case_comment_attachments_CommentId",
                schema: "Cases",
                table: "case_comment_attachments");

            migrationBuilder.RenameTable(
                name: "payment_records",
                schema: "Cases",
                newName: "payment_records");

            migrationBuilder.RenameTable(
                name: "investment_cases",
                schema: "Cases",
                newName: "investment_cases");

            migrationBuilder.RenameTable(
                name: "financial_worksheets",
                schema: "Cases",
                newName: "financial_worksheets");

            migrationBuilder.RenameTable(
                name: "case_workflow_history",
                schema: "Cases",
                newName: "case_workflow_history");

            migrationBuilder.RenameTable(
                name: "case_valuations",
                schema: "Cases",
                newName: "case_valuations");

            migrationBuilder.RenameTable(
                name: "case_revisions",
                schema: "Cases",
                newName: "case_revisions");

            migrationBuilder.RenameTable(
                name: "case_evaluations",
                schema: "Cases",
                newName: "case_evaluations");

            migrationBuilder.RenameTable(
                name: "case_evaluation_items",
                schema: "Cases",
                newName: "case_evaluation_items");

            migrationBuilder.RenameTable(
                name: "case_documents",
                schema: "Cases",
                newName: "case_documents");

            migrationBuilder.RenameTable(
                name: "case_data_entry_2",
                schema: "Cases",
                newName: "case_data_entry_2");

            migrationBuilder.RenameTable(
                name: "case_data_entry_1",
                schema: "Cases",
                newName: "case_data_entry_1");

            migrationBuilder.RenameTable(
                name: "case_comments",
                schema: "Cases",
                newName: "case_comments");

            migrationBuilder.RenameTable(
                name: "case_comment_attachments",
                schema: "Cases",
                newName: "CaseCommentAttachment");

            migrationBuilder.RenameIndex(
                name: "IX_case_comment_attachments_CaseCommentId",
                table: "CaseCommentAttachment",
                newName: "IX_CaseCommentAttachment_CaseCommentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CaseCommentAttachment",
                table: "CaseCommentAttachment",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseCommentAttachment_case_comments_CaseCommentId",
                table: "CaseCommentAttachment",
                column: "CaseCommentId",
                principalTable: "case_comments",
                principalColumn: "Id");
        }
    }
}
