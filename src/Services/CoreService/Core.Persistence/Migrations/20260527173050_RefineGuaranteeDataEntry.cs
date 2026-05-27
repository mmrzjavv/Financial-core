using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefineGuaranteeDataEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RepresentativeFullName",
                schema: "Cases",
                table: "guarantee_case_applications");

            migrationBuilder.DropColumn(
                name: "RepresentativeMobile",
                schema: "Cases",
                table: "guarantee_case_applications");

            migrationBuilder.AddColumn<int>(
                name: "ApplicantLegalForm",
                schema: "Cases",
                table: "guarantee_case_applications",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicantLegalForm",
                schema: "Cases",
                table: "guarantee_case_applications");

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeFullName",
                schema: "Cases",
                table: "guarantee_case_applications",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeMobile",
                schema: "Cases",
                table: "guarantee_case_applications",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }
    }
}
