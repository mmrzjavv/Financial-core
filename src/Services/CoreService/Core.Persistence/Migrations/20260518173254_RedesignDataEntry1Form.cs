using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RedesignDataEntry1Form : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessDescription",
                schema: "Cases",
                table: "case_data_entry_1");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "Cases",
                table: "case_data_entry_1");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "Cases",
                table: "case_data_entry_1");

            migrationBuilder.DropColumn(
                name: "Industry",
                schema: "Cases",
                table: "case_data_entry_1");

            migrationBuilder.DropColumn(
                name: "Website",
                schema: "Cases",
                table: "case_data_entry_1");

            migrationBuilder.RenameColumn(
                name: "TeamSize",
                schema: "Cases",
                table: "case_data_entry_1",
                newName: "BusinessStage");

            migrationBuilder.RenameColumn(
                name: "StartupTitle",
                schema: "Cases",
                table: "case_data_entry_1",
                newName: "RepresentativeFullName");

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                schema: "Cases",
                table: "case_data_entry_1",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactEmail",
                schema: "Cases",
                table: "case_data_entry_1");

            migrationBuilder.RenameColumn(
                name: "RepresentativeFullName",
                schema: "Cases",
                table: "case_data_entry_1",
                newName: "StartupTitle");

            migrationBuilder.RenameColumn(
                name: "BusinessStage",
                schema: "Cases",
                table: "case_data_entry_1",
                newName: "TeamSize");

            migrationBuilder.AddColumn<string>(
                name: "BusinessDescription",
                schema: "Cases",
                table: "case_data_entry_1",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "Cases",
                table: "case_data_entry_1",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "Cases",
                table: "case_data_entry_1",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Industry",
                schema: "Cases",
                table: "case_data_entry_1",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                schema: "Cases",
                table: "case_data_entry_1",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }
    }
}
