-- بازه زمانی سقف اعتبار صندوق (شروع دوره + انقضا)
ALTER TABLE "Cases"."guarantee_fund_credit_limit"
    ADD COLUMN IF NOT EXISTS "PeriodStart" date NOT NULL DEFAULT '2026-01-01',
    ADD COLUMN IF NOT EXISTS "ExpiresAt" date NOT NULL DEFAULT '2026-12-31';

UPDATE "Cases"."guarantee_fund_credit_limit"
SET "PeriodStart" = COALESCE("PeriodStart", '2026-01-01'),
    "ExpiresAt" = COALESCE("ExpiresAt", '2026-12-31')
WHERE "Id" = '00000000-0000-0000-0000-000000000001';
