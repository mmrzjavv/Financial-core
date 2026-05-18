using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropInvestmentCaseXminColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: "xmin" on PostgreSQL is a system column (transaction ID), not a user column.
            // It cannot be dropped (SQLSTATE 0A000). EF xmin/RowVersion mapping was removed in
            // RemoveInvestmentCaseRowVersion; document confirm no longer updates investment_cases.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
