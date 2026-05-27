using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuaranteeApplicantCreditProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guarantee_applicant_credit_profiles",
                schema: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreditLimitWithCheck = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LastSetByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantee_applicant_credit_profiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_applicant_credit_profiles_ApplicantUserId",
                schema: "Cases",
                table: "guarantee_applicant_credit_profiles",
                column: "ApplicantUserId",
                unique: true,
                filter: "\"CompanyId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_applicant_credit_profiles_CompanyId",
                schema: "Cases",
                table: "guarantee_applicant_credit_profiles",
                column: "CompanyId",
                unique: true,
                filter: "\"CompanyId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guarantee_applicant_credit_profiles",
                schema: "Cases");
        }
    }
}
