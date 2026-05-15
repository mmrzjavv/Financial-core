using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkInvestmentCaseToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                schema: "Cases",
                table: "investment_cases",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Cases"."investment_cases" ic
                SET "CompanyId" = c."Id"
                FROM "Identity"."Company" c
                WHERE ic."ApplicantType" = 2
                  AND ic.company_economic_code IS NOT NULL
                  AND ic.company_economic_code = c."EconomicCode"
                  AND ic."ApplicantUserId" = c."OwnerUserId"::text;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Cases"."investment_cases" ic
                SET "CompanyId" = c."Id"
                FROM "Identity"."Company" c
                WHERE ic."CompanyId" IS NULL
                  AND ic."ApplicantType" = 2
                  AND ic.company_name IS NOT NULL
                  AND ic.company_name = c."Name"
                  AND ic."ApplicantUserId" = c."OwnerUserId"::text;
                """);

            migrationBuilder.DropColumn(
                name: "company_address",
                schema: "Cases",
                table: "investment_cases");

            migrationBuilder.DropColumn(
                name: "company_city",
                schema: "Cases",
                table: "investment_cases");

            migrationBuilder.DropColumn(
                name: "company_economic_code",
                schema: "Cases",
                table: "investment_cases");

            migrationBuilder.DropColumn(
                name: "company_name",
                schema: "Cases",
                table: "investment_cases");

            migrationBuilder.DropColumn(
                name: "company_national_id",
                schema: "Cases",
                table: "investment_cases");

            migrationBuilder.DropColumn(
                name: "company_phone_number",
                schema: "Cases",
                table: "investment_cases");

            migrationBuilder.DropColumn(
                name: "company_postal_code",
                schema: "Cases",
                table: "investment_cases");

            migrationBuilder.DropColumn(
                name: "company_province",
                schema: "Cases",
                table: "investment_cases");

            migrationBuilder.DropColumn(
                name: "company_registration_number",
                schema: "Cases",
                table: "investment_cases");

            migrationBuilder.CreateIndex(
                name: "IX_investment_cases_CompanyId",
                schema: "Cases",
                table: "investment_cases",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_investment_cases_Company_CompanyId",
                schema: "Cases",
                table: "investment_cases",
                column: "CompanyId",
                principalSchema: "Identity",
                principalTable: "Company",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_investment_cases_Company_CompanyId",
                schema: "Cases",
                table: "investment_cases");

            migrationBuilder.DropIndex(
                name: "IX_investment_cases_CompanyId",
                schema: "Cases",
                table: "investment_cases");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "Cases",
                table: "investment_cases");

            migrationBuilder.AddColumn<string>(
                name: "company_address",
                schema: "Cases",
                table: "investment_cases",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_city",
                schema: "Cases",
                table: "investment_cases",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_economic_code",
                schema: "Cases",
                table: "investment_cases",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_name",
                schema: "Cases",
                table: "investment_cases",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_national_id",
                schema: "Cases",
                table: "investment_cases",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_phone_number",
                schema: "Cases",
                table: "investment_cases",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_postal_code",
                schema: "Cases",
                table: "investment_cases",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_province",
                schema: "Cases",
                table: "investment_cases",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_registration_number",
                schema: "Cases",
                table: "investment_cases",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }
    }
}
