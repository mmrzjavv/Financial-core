-- Migration: 20260527185900_AddGuaranteeApplicantCreditProfile
-- Run on PostgreSQL when dotnet ef database update cannot reach Liara.

START TRANSACTION;

CREATE TABLE IF NOT EXISTS "Cases".guarantee_applicant_credit_profiles (
    "Id" uuid NOT NULL,
    "ApplicantUserId" character varying(64) NOT NULL,
    "CompanyId" uuid,
    "CreditLimitWithCheck" numeric(18,2) NOT NULL,
    "LastSetByUserId" character varying(64),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_guarantee_applicant_credit_profiles" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_guarantee_applicant_credit_profiles_ApplicantUserId"
    ON "Cases".guarantee_applicant_credit_profiles ("ApplicantUserId")
    WHERE "CompanyId" IS NULL;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_guarantee_applicant_credit_profiles_CompanyId"
    ON "Cases".guarantee_applicant_credit_profiles ("CompanyId")
    WHERE "CompanyId" IS NOT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260527185900_AddGuaranteeApplicantCreditProfile', '9.0.13'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20260527185900_AddGuaranteeApplicantCreditProfile'
);

COMMIT;
