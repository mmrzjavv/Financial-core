using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixInvestmentCaseConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: Npgsql rowVersion maps to PostgreSQL's system column "xmin" (0A000 if dropped).
            // Concurrency on investment_cases is handled in app code: no RowVersion in the EF model,
            // InvestmentCaseUpdateSuppressorInterceptor, and ExecuteUpdate for status/phase changes.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
