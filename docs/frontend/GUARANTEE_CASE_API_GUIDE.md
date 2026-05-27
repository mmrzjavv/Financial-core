# Guarantee Case API Guide — Frontend Integration
# راهنمای یکپارچه‌سازی فرانت‌اند — پرونده ضمانت‌نامه

**Source of truth:** `GuaranteeCasesController`, `GuaranteeCaseStateManager`, `GuaranteeCaseAppService`

| Part | Language |
|------|----------|
| Part I — فارسی | Below |
| Part II — English | End of document |

---

## Part I — فارسی

### 1. خلاصه

- Base path: `/api/v1/guaranteecases`
- وضعیت فعلی را از `currentStatus` بخوانید (enum `GuaranteeCaseStatus`).
- انتقال‌های موفق معمولاً **HTTP 202** برمی‌گردانند.
- آپلود: `POST .../documents/presign` → PUT به S3 → `POST .../documents/confirm?s3Key=...`
- کلید S3: `guarantee-cases/{CaseNumber}/{DocumentType}/{version}{ext}`
- کارتابل یکپارچه: `GET /api/v1/kanban/action-required` (همه ماژول‌ها)

### 2. فلو صدور (۱۰ مرحله)

| # | وضعیت | مسئول | اقدام کلیدی |
|---|--------|--------|-------------|
| 1 | DataEntry (2) | متقاضی | `PUT /application`, آپلود مدارک, `POST /application/submit` |
| 2 | CreditReview (3) | اعتبارات | `POST /credit/approve` یا `revision-request` |
| 3 | ApprovalFormEntry (4) | اعتبارات | `PUT /approval-form`, `POST /approval-form/submit` |
| 4 | CeoApprovalInitial (5) | مدیرعامل | `POST /ceo/initial/approve` یا `reject` |
| 5 | WaitingDraftContract (6) | حقوقی | آپلود `DraftContract`, `POST /legal/draft-uploaded` |
| 6 | WaitingSignedContractAndAttachments (7) | متقاضی | `SignedContract` + پیوست‌ها, `POST /signed-package/submit` |
| 7 | FinancialAttachmentReview (8) | مالی | `POST /attachments/approve` یا `revision-request` |
| 8 | WaitingFinalContract (9) | حقوقی | `FinalContract`, `POST /legal/final-uploaded` |
| 9 | CeoApprovalFinal (10) | مدیرعامل | `POST /ceo/final/approve` / `reject` / `cancel` |
| 10 | WaitingIssuanceDocuments (11) | مالی | `GuaranteeInstrument` + `IssuanceReceipt`, `POST /issuance/uploaded` → Completed (12) |

### 3. ایجاد پرونده

```http
POST /api/v1/guaranteecases
Authorization: Bearer {token}
Content-Type: application/json

{ "applicantType": 2, "companyId": "guid-or-null" }
```

- `applicantType`: `1` حقیقی، `2` حقوقی (با `companyId` مالک جاری)
- فیلدهای شرکت/کاربر تکراری ذخیره نمی‌شوند؛ فقط `application` و فایل‌ها

### 4. فرم درخواست (`PUT /application`)

فیلدهای اصلی: `guaranteeType`, `contractSubject`, `isKnowledgeBasedProduct`, `beneficiaryName`, `beneficiaryNationalId`, `beneficiaryCompanyType`, `applicantCategory`, `applicantLegalForm`, `facilitySubject`, `requestedGuaranteeAmount`, `initialValidityDays`, `collateralDescription`, `baseContractNumber`, ...

نام شرکت، شناسه ملی و نماینده از **User/Company** خوانده می‌شود (در `application` ذخیره نمی‌شود).

### 5. مدارک الزامی قبل از Submit

پایه: `EstablishmentGazette`, `FinancialStatements3Years`, `ActivityLicenses`, `BankAccountTurnover`, `CreditInformationForm`, `CaseFormationFeeReceipt`, `GuaranteeIssuanceRequestForm`, `CeoBoardIdCards`

شرطی: `TenderAnnouncement` (مناقصه)، `BaseContractImage` (حسن انجام کار / پیش‌پرداخت)

### 6. نظرات داخلی

روی `POST /credit/approve` و `POST /attachments/approve` فیلد `internalComment` (مثل سرمایه‌گذاری). متقاضی `includeInternal=true` نمی‌بیند.

### 7. نقش‌ها

`CreditExpert` (50), `CreditManager` (51) — مجوز `guarantee_cases:credit_review`

### 8. کارتابل

```http
GET /api/v1/kanban/action-required
GET /api/v1/kanban/watching
```

کارت‌ها شامل: `module` (2=Guarantee), `apiBasePath`, `statusValue`, `statusKey`

مسیر legacy سرمایه‌گذاری (فقط IC): `GET /api/v1/investmentcases/kanban/...`

---

## Part II — English

### API index (guarantee issuance)

| Method | Path |
|--------|------|
| POST | `/api/v1/guaranteecases` |
| GET | `/api/v1/guaranteecases/{id}` |
| GET | `/api/v1/guaranteecases` (search query) |
| PUT | `/api/v1/guaranteecases/{id}/application` |
| POST | `/api/v1/guaranteecases/{id}/application/submit` |
| POST | `/api/v1/guaranteecases/{id}/credit/approve` |
| POST | `/api/v1/guaranteecases/{id}/credit/revision-request` |
| PUT | `/api/v1/guaranteecases/{id}/approval-form` |
| POST | `/api/v1/guaranteecases/{id}/approval-form/submit` |
| POST | `/api/v1/guaranteecases/{id}/ceo/initial/approve` |
| POST | `/api/v1/guaranteecases/{id}/legal/draft-uploaded` |
| POST | `/api/v1/guaranteecases/{id}/signed-package/submit` |
| POST | `/api/v1/guaranteecases/{id}/attachments/approve` |
| POST | `/api/v1/guaranteecases/{id}/legal/final-uploaded` |
| POST | `/api/v1/guaranteecases/{id}/ceo/final/approve` |
| POST | `/api/v1/guaranteecases/{id}/issuance/uploaded` |
| POST | `/api/v1/guaranteecases/{id}/documents/presign` |
| POST | `/api/v1/guaranteecases/{id}/documents/confirm` |

See [GUARANTEE_RENEWAL_API_GUIDE.md](./GUARANTEE_RENEWAL_API_GUIDE.md) for renewal flow.
