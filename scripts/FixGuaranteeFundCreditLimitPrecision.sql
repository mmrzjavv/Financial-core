-- اگر ذخیره سقف با خطای numeric field overflow می‌خورد، ستون را به numeric(18,2) گسترش دهید.
ALTER TABLE "Cases"."guarantee_fund_credit_limit"
    ALTER COLUMN "CreditLimitWithCheck" TYPE numeric(18,2)
    USING "CreditLimitWithCheck"::numeric(18,2);
