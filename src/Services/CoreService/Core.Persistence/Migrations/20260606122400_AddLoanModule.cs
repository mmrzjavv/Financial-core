using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Loan");

            migrationBuilder.CreateTable(
                name: "loan_cases",
                schema: "Loan",
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
                    table.PrimaryKey("PK_loan_cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_loan_cases_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "Identity",
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loan_approval_details",
                schema: "Loan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DebtToAssetRatio = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    CurrentRatio = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    ProfitabilityRatioPercent = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    CreditLimitWithCheck = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    IsCreditLineActive = table.Column<bool>(type: "boolean", nullable: true),
                    RemainingCreditAfterGrant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    FacilityType = table.Column<int>(type: "integer", nullable: true),
                    ContractSubject = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BrokerageAndRelatedContract = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ApprovedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ApprovedAmountInWords = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    RepaymentMonths = table.Column<int>(type: "integer", nullable: true),
                    GracePeriodMonths = table.Column<int>(type: "integer", nullable: true),
                    AnnualProfitRatePercent = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    DailyPenaltyRatePercent = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    CollateralDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    GuarantorsDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OtherNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ExpectedTotalProfit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    RepaymentCheckAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_approval_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_loan_approval_details_loan_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Loan",
                        principalTable: "loan_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loan_case_applications",
                schema: "Loan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    RequestedAmountInWords = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    FacilitySubject = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OfferedGuarantees = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ApplicantCategory = table.Column<int>(type: "integer", nullable: false),
                    ApplicantCategoryOther = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RepresentativePosition = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_case_applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_loan_case_applications_loan_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Loan",
                        principalTable: "loan_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loan_case_comments",
                schema: "Loan",
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
                    table.PrimaryKey("PK_loan_case_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_loan_case_comments_loan_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Loan",
                        principalTable: "loan_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loan_case_documents",
                schema: "Loan",
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
                    table.PrimaryKey("PK_loan_case_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_loan_case_documents_loan_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Loan",
                        principalTable: "loan_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loan_case_workflow_history",
                schema: "Loan",
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
                    table.PrimaryKey("PK_loan_case_workflow_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_loan_case_workflow_history_loan_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Loan",
                        principalTable: "loan_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loan_installments",
                schema: "Loan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    InstallmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProfitAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FundShareOfPrincipal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FundShareOfProfit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FundShareOfTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsGracePeriod = table.Column<bool>(type: "boolean", nullable: false),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReminderSentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_installments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_loan_installments_loan_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Loan",
                        principalTable: "loan_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loan_payments",
                schema: "Loan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TransactionNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ReceiptS3Key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StageNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_loan_payments_loan_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Loan",
                        principalTable: "loan_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_loan_approval_details_CaseId",
                schema: "Loan",
                table: "loan_approval_details",
                column: "CaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loan_case_applications_CaseId",
                schema: "Loan",
                table: "loan_case_applications",
                column: "CaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loan_case_comments_CaseId",
                schema: "Loan",
                table: "loan_case_comments",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_case_documents_CaseId_DocumentType_Version",
                schema: "Loan",
                table: "loan_case_documents",
                columns: new[] { "CaseId", "DocumentType", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_case_workflow_history_CaseId",
                schema: "Loan",
                table: "loan_case_workflow_history",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_case_workflow_history_CorrelationId",
                schema: "Loan",
                table: "loan_case_workflow_history",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_cases_ApplicantUserId",
                schema: "Loan",
                table: "loan_cases",
                column: "ApplicantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_cases_CaseNumber",
                schema: "Loan",
                table: "loan_cases",
                column: "CaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loan_cases_CompanyId",
                schema: "Loan",
                table: "loan_cases",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_cases_CreatedAt",
                schema: "Loan",
                table: "loan_cases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_loan_cases_CurrentStatus",
                schema: "Loan",
                table: "loan_cases",
                column: "CurrentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_loan_installments_CaseId_RowNumber",
                schema: "Loan",
                table: "loan_installments",
                columns: new[] { "CaseId", "RowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loan_installments_InstallmentDate_IsPaid_IsGracePeriod",
                schema: "Loan",
                table: "loan_installments",
                columns: new[] { "InstallmentDate", "IsPaid", "IsGracePeriod" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_payments_CaseId_StageNumber",
                schema: "Loan",
                table: "loan_payments",
                columns: new[] { "CaseId", "StageNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loan_approval_details",
                schema: "Loan");

            migrationBuilder.DropTable(
                name: "loan_case_applications",
                schema: "Loan");

            migrationBuilder.DropTable(
                name: "loan_case_comments",
                schema: "Loan");

            migrationBuilder.DropTable(
                name: "loan_case_documents",
                schema: "Loan");

            migrationBuilder.DropTable(
                name: "loan_case_workflow_history",
                schema: "Loan");

            migrationBuilder.DropTable(
                name: "loan_installments",
                schema: "Loan");

            migrationBuilder.DropTable(
                name: "loan_payments",
                schema: "Loan");

            migrationBuilder.DropTable(
                name: "loan_cases",
                schema: "Loan");
        }
    }
}
