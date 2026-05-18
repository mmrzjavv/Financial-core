using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendCompanyProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "Identity",
                table: "Company",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EconomicCode",
                schema: "Identity",
                table: "Company",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                schema: "Identity",
                table: "Company",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "Identity",
                table: "Company",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                schema: "Identity",
                table: "Company",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                schema: "Identity",
                table: "Company",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Company_OwnerUserId",
                schema: "Identity",
                table: "Company",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Company_User_OwnerUserId",
                schema: "Identity",
                table: "Company",
                column: "OwnerUserId",
                principalSchema: "Identity",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Company_User_OwnerUserId",
                schema: "Identity",
                table: "Company");

            migrationBuilder.DropIndex(
                name: "IX_Company_OwnerUserId",
                schema: "Identity",
                table: "Company");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "Identity",
                table: "Company");

            migrationBuilder.DropColumn(
                name: "EconomicCode",
                schema: "Identity",
                table: "Company");

            migrationBuilder.DropColumn(
                name: "NationalId",
                schema: "Identity",
                table: "Company");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "Identity",
                table: "Company");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                schema: "Identity",
                table: "Company");

            migrationBuilder.DropColumn(
                name: "Province",
                schema: "Identity",
                table: "Company");
        }
    }
}
