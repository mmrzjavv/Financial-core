using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuaranteeModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guarantee_cases",
                schema: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ApplicantUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ApplicantType = table.Column<int>(type: "integer", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentPhase = table.Column<int>(type: "integer", nullable: false),
                    CurrentStatus = table.Column<int>(type: "integer", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantee_cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guarantee_cases_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "Identity",
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "guarantee_approval_forms",
                schema: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditLimitWithCheck = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    FundIssuedGuaranteesTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ActiveCommitments = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    RemainingCredit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    GuaranteeType = table.Column<int>(type: "integer", nullable: true),
                    GuaranteeAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    GuaranteeAmountInWords = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ContractSubject = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Beneficiary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IssuanceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ActiveDurationDays = table.Column<int>(type: "integer", nullable: true),
                    DepositRatePercent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    AnnualCommissionRatePercent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CollateralDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    GuarantorsDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OtherNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantee_approval_forms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guarantee_approval_forms_guarantee_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Cases",
                        principalTable: "guarantee_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guarantee_case_applications",
                schema: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuaranteeType = table.Column<int>(type: "integer", nullable: true),
                    ContractSubject = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsKnowledgeBasedProduct = table.Column<bool>(type: "boolean", nullable: true),
                    BeneficiaryName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    BeneficiaryNationalId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    BeneficiaryCompanyType = table.Column<int>(type: "integer", nullable: true),
                    ApplicantCategory = table.Column<int>(type: "integer", nullable: false),
                    ApplicantCategoryOther = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    BaseContractNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BaseContractAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    BaseContractAmountInWords = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    PriceAdjustmentRatePercent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    ExecutionProvince = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RequestedGuaranteeAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    InitialValidityDays = table.Column<int>(type: "integer", nullable: true),
                    ValidityFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidityTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CollateralDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FacilitySubject = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RepresentativeFullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RepresentativeMobile = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantee_case_applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guarantee_case_applications_guarantee_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Cases",
                        principalTable: "guarantee_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guarantee_case_comments",
                schema: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phase = table.Column<int>(type: "integer", nullable: false),
                    SenderUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SenderRole = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IsRevisionRequest = table.Column<bool>(type: "boolean", nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantee_case_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guarantee_case_comments_guarantee_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Cases",
                        principalTable: "guarantee_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guarantee_case_documents",
                schema: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    S3Key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantee_case_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guarantee_case_documents_guarantee_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Cases",
                        principalTable: "guarantee_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guarantee_case_workflow_history",
                schema: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromPhase = table.Column<int>(type: "integer", nullable: false),
                    ToPhase = table.Column<int>(type: "integer", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: false),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorRole = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantee_case_workflow_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guarantee_case_workflow_history_guarantee_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Cases",
                        principalTable: "guarantee_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guarantee_renewal_cases",
                schema: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ApplicantUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ParentGuaranteeCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RenewalKind = table.Column<int>(type: "integer", nullable: false),
                    CurrentStatus = table.Column<int>(type: "integer", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RequestedExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RequestedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ApprovedExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantee_renewal_cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guarantee_renewal_cases_guarantee_cases_ParentGuaranteeCase~",
                        column: x => x.ParentGuaranteeCaseId,
                        principalSchema: "Cases",
                        principalTable: "guarantee_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_investment_cases_CreatedAt",
                schema: "Cases",
                table: "investment_cases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_investment_cases_CurrentStatus",
                schema: "Cases",
                table: "investment_cases",
                column: "CurrentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_approval_forms_CaseId",
                schema: "Cases",
                table: "guarantee_approval_forms",
                column: "CaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_case_applications_CaseId",
                schema: "Cases",
                table: "guarantee_case_applications",
                column: "CaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_case_comments_CaseId",
                schema: "Cases",
                table: "guarantee_case_comments",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_case_documents_CaseId_DocumentType_Version",
                schema: "Cases",
                table: "guarantee_case_documents",
                columns: new[] { "CaseId", "DocumentType", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_case_workflow_history_CaseId",
                schema: "Cases",
                table: "guarantee_case_workflow_history",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_cases_ApplicantUserId",
                schema: "Cases",
                table: "guarantee_cases",
                column: "ApplicantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_cases_CaseNumber",
                schema: "Cases",
                table: "guarantee_cases",
                column: "CaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_cases_CompanyId",
                schema: "Cases",
                table: "guarantee_cases",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_cases_CreatedAt",
                schema: "Cases",
                table: "guarantee_cases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_cases_CurrentStatus",
                schema: "Cases",
                table: "guarantee_cases",
                column: "CurrentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_renewal_cases_CaseNumber",
                schema: "Cases",
                table: "guarantee_renewal_cases",
                column: "CaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_renewal_cases_ParentGuaranteeCaseId",
                schema: "Cases",
                table: "guarantee_renewal_cases",
                column: "ParentGuaranteeCaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guarantee_approval_forms",
                schema: "Cases");

            migrationBuilder.DropTable(
                name: "guarantee_case_applications",
                schema: "Cases");

            migrationBuilder.DropTable(
                name: "guarantee_case_comments",
                schema: "Cases");

            migrationBuilder.DropTable(
                name: "guarantee_case_documents",
                schema: "Cases");

            migrationBuilder.DropTable(
                name: "guarantee_case_workflow_history",
                schema: "Cases");

            migrationBuilder.DropTable(
                name: "guarantee_renewal_cases",
                schema: "Cases");

            migrationBuilder.DropTable(
                name: "guarantee_cases",
                schema: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_investment_cases_CreatedAt",
                schema: "Cases",
                table: "investment_cases");

            migrationBuilder.DropIndex(
                name: "IX_investment_cases_CurrentStatus",
                schema: "Cases",
                table: "investment_cases");
        }
    }
}
