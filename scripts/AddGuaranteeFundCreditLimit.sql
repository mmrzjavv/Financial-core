-- سقف اعتبار کل صندوق برای ضمانت‌نامه (یک رکورد سراسری)
CREATE TABLE IF NOT EXISTS "Cases"."guarantee_fund_credit_limit" (
    "Id" uuid NOT NULL,
    "CreditLimitWithCheck" numeric(18,2) NOT NULL,
    "PeriodStart" date NOT NULL DEFAULT '2026-01-01',
    "ExpiresAt" date NOT NULL DEFAULT '2026-12-31',
    "LastSetByUserId" character varying(64),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_guarantee_fund_credit_limit" PRIMARY KEY ("Id")
);

INSERT INTO "Cases"."guarantee_fund_credit_limit" ("Id", "CreditLimitWithCheck", "PeriodStart", "ExpiresAt", "LastSetByUserId", "CreatedAt", "UpdatedAt")
VALUES ('00000000-0000-0000-0000-000000000001', 0, '2026-01-01', '2026-12-31', 'system', timezone('utc', now()), timezone('utc', now()))
ON CONFLICT ("Id") DO NOTHING;
