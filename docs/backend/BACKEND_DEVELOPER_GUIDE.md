# راهنمای توسعه‌دهنده بک‌اند — Financial-Core

**مخاطب:** برنامه‌نویس بک‌اند تازه‌وارد که قرار است این ریپو را نگه‌داری و توسعه دهد.  
**هدف:** جایگزین «گشتن در کل پروژه» — مسیرها، قراردادها، و چک‌لیست‌های عملی برای تغییرات روزمره.  
**آخرین هم‌راستاسازی با کد:** بهار ۱۴۰۵ (نسخه API v1، مسیرهای `investmentcases` و `identity`).

> برای قرارداد HTTP و فلو پرونده سرمایه‌گذاری از نگاه فرانت، ببینید: [`docs/frontend/INVESTMENT_CASE_API_GUIDE.md`](../frontend/INVESTMENT_CASE_API_GUIDE.md).

> **تغییر دسترسی نقش (Permission / Role):** مرجع کامل در [**بخش ۹ تا ۱۳**](#9-مجوزدهی--سه-لایه-و-نقشه-فایل‌ها) — نقشه فایل‌ها، چک‌لیست اضافه/حذف، فهرست همه permissionها، Policyها، کش و JWT.

---

## فهرست

1. [نگاه کلی محصول و معماری](#1-نگاه-کلی-محصول-و-معماری)
2. [ساختار Solution و پوشه‌ها](#2-ساختار-solution-و-پوشه‌ها)
3. [لایه‌ها و قوانین وابستگی](#3-لایه‌ها-و-قوانین-وابستگی)
4. [BuildingBlocks — زیرساخت مشترک](#4-buildingblocks--زیرساخت-مشترک)
5. [پروژه Core Service — نقش هر Assembly](#5-پروژه-core-service--نقش-هر-assembly)
6. [جریان یک درخواست HTTP](#6-جریان-یک-درخواست-http)
7. [مسیرهای API (Route)](#7-مسیرهای-api-route)
8. [احراز هویت، JWT و Claimها](#8-احراز-هویت-jwt-و-claimها)
9. [مجوزدهی — دو لایه جدا](#9-مجوزدهی--دو-لایه-جدا)
10. [نقش (Role) — اضافه، تغییر، حذف دسترسی](#10-نقش-role--اضافه-تغییر-حذف-دسترسی)
11. [سیاست‌های ASP.NET در Program.cs](#11-سیاست‌های-aspnet-در-programcs)
12. [کش Permission و نکته مهم بعد از تغییر نقش](#12-کش-permission-و-نکته-مهم-بعد-از-تغییر-نقش)
13. [ریپازیتوری و دیتابیس](#13-ریپازیتوری-و-دیتابیس)
14. [چک‌لیست: اضافه کردن Entity جدید](#14-چک‌لیست-اضافه-کردن-entity-جدید)
15. [Migration EF Core](#15-migration-ef-core)
16. [Unit of Work و SaveChanges](#16-unit-of-work-و-savechanges)
17. [Workflow (Elsa) و پرونده سرمایه‌گذاری](#17-workflow-elsa-و-پرونده-سرمایه‌گذاری)
18. [App Service، Validator، DTO و Mapster](#18-app-service-validator-dto-و-mapster)
19. [Controller و پاسخ API](#19-controller-و-پاسخ-api)
20. [پیکربندی (appsettings)](#20-پیکربندی-appsettings)
21. [لاگ، Observability و خطاها](#21-لاگ-observability-و-خطاها)
22. [کارهای رایج نگه‌داری — چک‌لیست سریع](#22-کارهای-رایج-نگه‌داری--چک‌لیست-سریع)
23. [فایل‌های مرجع (Index)](#23-فایل‌های-مرجع-index)

---

## 1. نگاه کلی محصول و معماری

**Financial-Core** بک‌اند پلتفرم عملیات صندوق است: ماژول اصلی فعلی **پرونده سرمایه‌گذاری (Investment Case)** است — ورود داده، گردش کار، ارزیابی، قرارداد، کاربرگ مالی، تأیید مدیرعامل، پرداخت، سند، کامنت و کانبان.

الگوی کلی: **Clean Architecture سبک** با جداسازی:

| لایه | مسئولیت |
|------|---------|
| **API** | HTTP، Auth attribute، Versioning، Swagger |
| **Application** | Use case، Validator، Authorization سطح دامنه پرونده، DTO |
| **Domain** | Entity، Enum، رویداد دامنه |
| **Infrastructure** | پیاده‌سازی Repository، S3، SMS، Identity helpers |
| **Persistence** | EF Core `DbContext`، Configuration، Migration |
| **Workflow** | Elsa workflow پرونده |

دیتابیس: **PostgreSQL** (اسکیمای `Identity` و `Cases`). کش توزیع‌شده: **Redis** (OTP، Session، Permission — در صورت تنظیم connection string).

---

## 2. ساختار Solution و پوشه‌ها

Solution اصلی: `InvestmentCaseManagement.sln` (ریشه ریپو).

```
Financial-Core/
├── src/
│   ├── BuildingBlocks/           # کتابخانه‌های مشترک بین سرویس‌ها
│   │   ├── BuildingBlocks.Domain
│   │   ├── BuildingBlocks.Application
│   │   ├── BuildingBlocks.Contracts
│   │   ├── BuildingBlocks.Persistence
│   │   ├── BuildingBlocks.Infrastructure
│   │   └── BuildingBlocks.Observability
│   └── Services/
│       └── CoreService/
│           ├── Core.API              # Host، Controllers، Program.cs
│           ├── Core.Application      # سرویس‌های کاربردی، Permissions، DTO
│           ├── Core.Domain           # Entity و Enum دامنه پرونده + Identity
│           ├── Core.Infrastructure   # Repository، Storage، SMS
│           ├── Core.Persistence      # DbContext، Configurations، Migrations
│           └── Core.Workflow         # Elsa + InvestmentCaseWorkflow
├── Frontend/                       # پنل تست/مرجع (JS) — نه production SPA
├── docs/
│   ├── backend/                    # همین سند
│   └── frontend/                   # راهنمای API برای فرانت
└── InvestmentCaseManagement.sln
```

**نکته:** نام Solution تاریخی است (`InvestmentCaseManagement`)؛ نام محصول/سرویس در لاگ‌ها `Financial.Core` است.

---

## 3. لایه‌ها و قوانین وابستگی

جهت وابستگی مجاز (خلاف آن = بوی بد معماری):

```
Core.API  →  Application, Infrastructure, Persistence, Workflow, BuildingBlocks.*
Core.Infrastructure  →  Application, Persistence, Domain, BuildingBlocks.*
Core.Persistence  →  Application (فقط ICoreDbContext), Domain, BuildingBlocks.Persistence
Core.Application  →  Domain, BuildingBlocks.Application
Core.Domain  →  BuildingBlocks.Domain (و گاهی بدون وابستگی سنگین)
Core.Workflow  →  Application, Domain
```

| قانون | توضیح |
|-------|--------|
| Domain به Application/API وابسته **نشود** | Entity فقط منطق دامنه و invariant |
| Controller منطق کسب‌وکار ندارد | فقط delegate به `*AppService` |
| EF و `DbContext` در API expose **نشود** | از Repository / `ICoreUnitOfWork` استفاده شود |
| Query سنگین در Controller **نباشد** | داخل Repository یا AppService |

---

## 4. BuildingBlocks — زیرساخت مشترک

| پروژه | محتوای مهم |
|-------|------------|
| **Domain** | `Entity<TKey>`, `AuditableEntity`, `SoftDeletableEntity`, `IDomainEvent`, `IUserContext` |
| **Application** | `Result<T>`, `ApiOperationResult<T>`, `IClock`, اعتبارسنجی مشترک |
| **Persistence** | `DbContextBase`, Interceptorهای Audit و SoftDelete، `IWriteRepository<,>` |
| **Infrastructure** | S3، DI مشترک |
| **Observability** | Serilog، CorrelationId middleware |
| **Contracts** | `PagedResult` و قراردادهای سبک |

### `IUserContext` (خواندن کاربر جاری در Application)

تعریف: `BuildingBlocks.Domain.Abstractions.IUserContext`

پیاده‌سازی در API: `Core.API.HttpUserContext` (ثبت در `Program.cs`):

- `UserId` ← `ClaimTypes.NameIdentifier`
- `UserName` ← `ClaimTypes.Name`
- `Roles` ← همه claimهای `ClaimTypes.Role`

**تفاوت با Identity:** برای OTP/Session از `ICurrentUserAccessor` در `Core.Infrastructure.Identity` استفاده می‌شود (`Guid?` برای UserId و SessionId از claim `sid`).

---

## 5. پروژه Core Service — نقش هر Assembly

### Core.API
- `Program.cs`: DI، JWT، CORS، Versioning، Authorization policies، Pipeline
- Controllers: `InvestmentCasesController`, `UserController`, `CompaniesController`, `DashboardController`
- `PermissionAuthorizationHandler`: بررسی permission روی JWT role + fallback به `IIdentityClient`
- Swagger در `Core.API/Swagger/`

### Core.Application
- **Use cases:** `InvestmentCaseAppService`, `KanbanAppService`, `CompanyAppService`, `UserService`, …
- **Authorization پرونده:** `CaseAuthorizationService` + `CasePermissions` (رشته‌های `cases:*`)
- **Authorization API/نقش:** `Permissions` + `RolePermissions` (رشته‌های `users:*`, `investment_cases:*`, …)
- **State machine:** `CaseStateManager` — انتقال وضعیت پرونده
- Abstractions: `IInvestmentCaseRepository`, `ICoreUnitOfWork`, `ICoreDbContext`, …

### Core.Domain
- `Entities/`: `InvestmentCase`, `CaseDocument`, `PaymentRecord`, …
- `Identity/`: `UserRole` enum, `UserRoleClaims`, Entityهای `User`, `Company`, …
- `Enums/`: `CaseStatus`, `CasePhase`, `DocumentType`, …

### Core.Infrastructure
- پیاده‌سازی Repositoryها (`InvestmentCaseRepository`, `CompanyRepository`, …)
- `EfRepository<TEntity,TKey>` برای CRUD جنریک
- Liara S3، SMS queue، `CoreUnitOfWork`
- Identity: `TokenHelper`, `DistributedPermissionCacheService`, Redis OTP/Session

### Core.Persistence
- `CoreDbContext` + `Configurations/*`
- Migrations در `Migrations/`
- `DbSchemas`: `Identity`, `Cases`
- Interceptor اختصاصی: `InvestmentCaseUpdateSuppressorInterceptor` (جلوگیری از concurrency ناخواست روی parent هنگام ذخیره سند)

### Core.Workflow
- Elsa workflow: `InvestmentCaseWorkflow`
- `ElsaCaseWorkflowOrchestrator` — شروع/ادامه instance و اتصال به `InvestmentCase.WorkflowInstanceId`

---

## 6. جریان یک درخواست HTTP

```
Client
  → JWT Bearer (رمزگذاری‌شده)
  → Authentication (JwtBearer)
  → Authorization ([Authorize], Policy)
  → Controller
  → AppService
       → CaseAuthorizationService / PermissionService (در صورت نیاز)
       → Repository / UnitOfWork
       → SaveChangesAsync
       → Workflow orchestrator (برای transition)
  → Result / ApiOperationResult
  → ApiResponse (JSON envelope یکسان)
```

**Envelope پاسخ:** `ApiOperationResult<T>` با فیلدهایی مثل `success`, `message`, `data`, `errors` — از `ApiControllerBase.Respond` استفاده کنید.

---

## 7. مسیرهای API (Route)

پیشوند نسخه: `api/v{version}` (پیش‌فرض `1.0`).

| حوزه | Route پایه | Controller |
|------|------------|------------|
| پرونده سرمایه‌گذاری | `/api/v1/investmentcases` | `InvestmentCasesController` |
| کاربر / OTP | `/api/v1/identity/users` | `UserController` |
| شرکت متقاضی | `/api/v1/identity/companies` | `CompaniesController` |
| داشبورد | `/api/v1/dashboard` | `DashboardController` |

زیرمسیرهای پرونده (نمونه):  
`GET /investmentcases/{id}`, `PUT .../data-entry1`, `POST .../data-entry1/submit`, `GET .../kanban/action-required`, `POST .../documents/presign`, …

**Swagger:** بعد از `dotnet run` روی Core.API → `/swagger`.

---

## 8. احراز هویت، JWT و Claimها

### صدور توکن
کلاس: `Core.Infrastructure.Identity.Identity.TokenHandler.TokenHelper`

Claimهای داخل access token:

| Claim | نوع استاندارد | محتوا |
|-------|----------------|--------|
| شناسه کاربر | `ClaimTypes.NameIdentifier` | `User.Id` (Guid رشته‌ای) |
| نام کاربری | `ClaimTypes.Name` | معمولاً شماره موبایل |
| نقش | `ClaimTypes.Role` | **یک** نقش: `UserRole.ToString()` مثلاً `Applicant`, `InvestmentExpert`, `CEO` |
| نشست | `sid` | SessionId برای چند دستگاه |
| داده اضافه | `ClaimTypes.UserData` | JSON (`UserDataClaim`) |

نقش در توکن از `RoleClaimMapper.ToClaimRole(user.Role)` می‌آید — همان نام enum به‌صورت رشته.

### خواندن Claim در کد

```csharp
// در لایه Application (پرونده، کانبان، …)
public sealed class CaseAuthorizationService(IUserContext userContext)
{
    var userId = userContext.UserId;
    var isAdmin = userContext.Roles.Contains(UserRoleClaims.Admin);
}

// در لایه Identity (مثلاً UserService)
public sealed class HttpCurrentUserAccessor(IHttpContextAccessor accessor) : ICurrentUserAccessor
{
    // ClaimTypes.NameIdentifier → Guid?
    // "sid" → Guid?
}
```

### Helper قدیمی (Infrastructure)
`ClaimHelper.GetClaimData(claims)` — برای استخراج `UserTokenData` از `IEnumerable<Claim>`؛ در مسیرهای قدیمی‌تر Identity استفاده می‌شود.

### OTP و Session
- `POST /identity/users/send-otp` → Redis/Memory cache OTP
- `POST /identity/users/verify-otp` → JWT + Refresh token
- `POST /identity/users/refresh-token` — هدر `Authorization: Bearer {access}` لازم است
- Session در Redis با کلیدهای مربوط به `AuthSessionOptions`

### نقش‌های Legacy در JWT
`UserRoleClaims.Normalize` aliasها را یکسان می‌کند:

| Alias قدیمی | نقش واقعی |
|-------------|-----------|
| `LegalUnit` | `LegalExpert` |
| `FinancialUnit` | `FinancialExpert` |
| `InvestmentUnit` | `InvestmentExpert` |
| `User` | `Applicant` |
| `CEO` (رشته) | در Policyها جداگانه هم پذیرفته می‌شود |

---

## 9. مجوزدهی — سه لایه و نقشه فایل‌ها

> **یادآوری روز نیاز:** دسترسی نقش‌ها در **کد** است، نه جدول دیتابیس. برای یک تغییر معمولاً **تا سه فایل** را ویرایش می‌کنید (بخش ۱۰). فقط `Permissions.cs` برای همه چیز کافی **نیست**.

### اصل مهم

| اشتباه رایج | واقعیت |
|-------------|--------|
| «فقط `Permissions.cs` را عوض کنم» | فقط **لایه API** عوض می‌شود |
| `investment_cases:review` = `cases:manage_valuations` | **سه فضای نام جدا** — رشته‌ها متفاوتند |
| Admin Panel برای tick کردن permission | **وجود ندارد** — آرایه C# در سه فایل |
| بعد از deploy فوراً اثر می‌کند | کش Redis ۳۰ دقیقه + JWT قدیمی (بخش ۱۳) |

### سه لایه — یک نگاه

```
درخواست HTTP
    │
    ├─► [لایه ۱] Policy روی Controller ──► PermissionService ──► Permissions.cs
    │
    └─► AppService
            ├─► [لایه ۲] سرمایه‌گذاری ──► CaseAuthorizationService.cs
            └─► [لایه ۳] ضمانت‌نامه ──► GuaranteeAuthorizationService.cs
```

| لایه | فایل(ها) | فرمت رشته | چه چیزی را کنترل می‌کند |
|------|----------|-----------|-------------------------|
| **۱ — API** | `Identity/Authorization/Permissions.cs` | `users:read`, `guarantee_cases:credit_review` | `[Authorize(Policy)]` روی Controller |
| **۲ — پرونده سرمایه‌گذاری** | `Authorization/CasePermissions.cs` + `CaseAuthorizationService.cs` | `cases:manage_payments` | منطق داخل `InvestmentCaseAppService`, `PaymentService`, `ReviewService`, … |
| **۳ — پرونده ضمانت** | `Authorization/GuaranteePermissions.cs` + `GuaranteeAuthorizationService.cs` | `guarantee_cases:manage_approval_form` | منطق داخل `GuaranteeCaseAppService`, … |

**Admin:** در هر سه لایه تقریباً همیشه bypass دارد (`UserRole.Admin` / `UserRoleClaims.Admin` → `true`).

### نقشه فایل‌ها (کپی برای روز کاری)

| کار | فایل |
|-----|------|
| تعریف نقش (enum) | `Core.Domain/Identity/UserRole.cs` |
| Permission API + نقش → API | `Core.Application/Identity/Authorization/Permissions.cs` |
| Permission سرمایه‌گذاری | `Core.Application/Authorization/CasePermissions.cs` |
| نقش → permission سرمایه‌گذاری | `Core.Application/Authorization/CaseAuthorizationService.cs` |
| Permission ضمانت | `Core.Application/Authorization/GuaranteePermissions.cs` |
| نقش → permission ضمانت | `Core.Application/Authorization/GuaranteeAuthorizationService.cs` |
| ثبت Policyهای HTTP | `Core.API/DependencyInjection/AuthorizationServiceCollectionExtensions.cs` |
| Handler بررسی permission در Policy | `Core.API/Authorization/PermissionAuthorizationHandler.cs` |
| کش permission کاربر | `Core.Infrastructure/.../DistributedPermissionCacheService.cs` |
| خواندن permission از DB نقش | `Core.Application/Identity/Services/Authorization/PermissionService.cs` |

### جریان تصمیم — «کدام فایل را باز کنم؟»

| سناریو | فایل‌ها |
|--------|---------|
| Endpoint جدید 403 می‌دهد | `Permissions.cs` + `AuthorizationServiceCollectionExtensions.cs` + Controller |
| داخل سرویس `HasPermission` برای **سرمایه‌گذاری** false است | `CaseAuthorizationService.cs` (+ شاید `CasePermissions.cs`) |
| داخل `GuaranteeCaseAppService` مثلاً credit limit | `GuaranteeAuthorizationService.cs` (+ شاید `GuaranteePermissions.cs`) |
| فقط «متقاضی / داخلی» روی route | `AuthorizationServiceCollectionExtensions.cs` (`ApplicantOnly` / `InternalOnly`) — **بدون** permission string |
| تأیید CEO روی route | اغلب **نقش** (`Ceo`/`CEO`/`Admin`) نه فقط permission — بخش ۱۲ |

### آرایه‌های مشترک (کم‌کردن تکرار)

در `Permissions.cs`: `LegalUnitPermissions`, `FinancialUnitPermissions`, `TechnicalUnitPermissions` — چند نقش به یک آرایه اشاره می‌کنند.

در `CaseAuthorizationService.cs` / `GuaranteeAuthorizationService.cs`: همین الگو (`InvestmentExpertPermissions`, `CreditUnitPermissions`, …).

برای **دادن همه دسترسی‌ها به یک نقش** (فقط dev/test):

```csharp
// لایه ۱
[UserRoleClaims.TechnicalExpert] = RolePermissions.AllPermissions,

// لایه ۲ — از AllCasePermissions در CaseAuthorizationService استفاده کنید
[UserRoleClaims.TechnicalExpert] = AllCasePermissions,

// لایه ۳ — از AllGuaranteePermissions در GuaranteeAuthorizationService
[UserRoleClaims.TechnicalExpert] = AllGuaranteePermissions,
```

> در کد فعلی ممکن است نمونه `TechnicalExpert` با دسترسی کامل برای تست باشد؛ قبل از production به آرایه‌های واحد تخصصی (`TechnicalUnitPermissions`) برگردانید.

---

## 10. راهنمای عملی: اضافه / حذف Permission برای نقش

### مدل نقش در دیتابیس

- Enum: `Core.Domain.Identity.UserRole` — **مقادیر عددی موجود را عوض نکنید**؛ فقط مقدار جدید با عدد آزاد
- ستون: `Identity.User.Role` (int)
- رشته در JWT: `UserRoleClaims` در `UserRole.cs`
- تغییر نقش کاربر: `PUT /identity/users/{id}` با `Role` — فقط Admin

---

### A) فقط **اضافه کردن** یک permission موجود به نقش موجود

مثال: به `CreditManager` اجازه `guarantee_cases:set_applicant_credit_limit` در API بدهید.

**گام ۱ — لایه API** (`Permissions.cs`):

```csharp
[UserRoleClaims.CreditManager] =
[
    // ... موجودها
    Permissions.GuaranteeCases_SetApplicantCreditLimit,  // ← اضافه
],
```

**گام ۲ — لایه ضمانت** (`GuaranteeAuthorizationService.cs`) — چون `GuaranteeCaseAppService` از `GuaranteePermissions.SetApplicantCreditLimit` استفاده می‌کند:

```csharp
// یا به CreditUnitPermissions اضافه کنید، یا آرایه جدا برای CreditManager
GuaranteePermissions.SetApplicantCreditLimit,
```

**گام ۳ — لایه سرمایه‌گذاری:** اگر این feature مربوط سرمایه‌گذاری نیست، **نیازی نیست**.

**گام ۴:** Build، Deploy، کاربر **re-login**، پاک کردن کش (بخش ۱۳).

---

### B) **حذف** permission از نقش

همان خطوط را از آرایه نقش در **هر لایه‌ای که اضافه کرده بودید** حذف کنید. اگر فقط از `Permissions.cs` حذف کنید، ممکن است AppService هنوز اجازه بدهد (یا برعکس).

---

### C) تعریف permission **کاملاً جدید** (end-to-end)

| مرحله | لایه API | لایه سرمایه‌گذاری | لایه ضمانت |
|-------|----------|-------------------|------------|
| ۱. ثابت | `Permissions.MyFeature = "module:action"` | `CasePermissions.MyFeature = "cases:..."` | `GuaranteePermissions.MyFeature = "guarantee_cases:..."` |
| ۲. Admin / همه | `AllPermissions` += ثابت | `AllCasePermissions` (در صورت وجود) | `AllGuaranteePermissions` |
| ۳. نقش‌ها | `RolePermissionMappings[نقش]` | `RolePermissions[نقش]` در CaseAuthorizationService | همان در GuaranteeAuthorizationService |
| ۴. استفاده | `[Authorize(Policy = "...")]` + Policy در `AuthorizationServiceCollectionExtensions.cs` | `caseAuth.HasPermission(CasePermissions.MyFeature)` | `guaranteeAuth.HasPermission(...)` |

---

### D) اضافه کردن **نقش جدید** (مثلاً `RiskManager`)

1. `UserRole.cs` — `RiskManager = 52` + `UserRoleClaims.RiskManager`
2. `Permissions.cs` — ورودی در `RolePermissionMappings`
3. `CaseAuthorizationService.cs` — در صورت نیاز به سرمایه‌گذاری
4. `GuaranteeAuthorizationService.cs` — در صورت نیاز به ضمانت
5. `AuthorizationServiceCollectionExtensions.cs` — اگر internal است → `InternalOnly`
6. `CaseAuthorizationService.IsInternalUser` — اگر داخلی است → نام نقش را اضافه کنید
7. `Frontend/config.js` / `workflow-model.js` — persona تست (اختیاری)
8. **Migration لازم نیست** (فقط enum)

---

### E) چک‌لیست یک‌صفحه‌ای بعد از هر تغییر دسترسی

- [ ] هر سه لایه مرتبط ویرایش شد؟
- [ ] `dotnet build` روی `Core.API`
- [ ] کاربر تست همان `Role` در DB دارد؟
- [ ] Logout / Login مجدد (JWT)
- [ ] Redis: `DEL permissions:user:{guid}` یا ۳۰ دقیقه صبر
- [ ] Swagger یا Frontend با همان نقش تست شد
- [ ] اگر 403 روی route خاص → Policy بخش ۱۲ (شاید role-only باشد نه permission)

---

## 11. فهرست کامل Permissionها و نقش‌ها

> منبع حقیقت همیشه **کد** است؛ این جدول برای جستجوی سریع است. بعد از تغییر کد، جدول را در همین سند به‌روز کنید.

### لایه ۱ — API (`Permissions.cs`)

| ثابت C# | رشته |
|---------|------|
| `Users_Read` | `users:read` |
| `Users_Write` | `users:write` |
| `Users_Delete` | `users:delete` |
| `Users_ManageRoles` | `users:manage_roles` |
| `Companies_Read` | `companies:read` |
| `Companies_Write` | `companies:write` |
| `Companies_Delete` | `companies:delete` |
| `Sessions_Read` | `sessions:read` |
| `Sessions_Write` | `sessions:write` |
| `Sessions_Revoke` | `sessions:revoke` |
| `Otp_Send` | `otp:send` |
| `Otp_Verify` | `otp:verify` |
| `Admin_FullAccess` | `admin:full_access` |
| `InvestmentCases_Read` | `investment_cases:read` |
| `InvestmentCases_Write` | `investment_cases:write` |
| `InvestmentCases_Review` | `investment_cases:review` |
| `InvestmentCases_FinanceReview` | `investment_cases:finance_review` |
| `InvestmentCases_LegalReview` | `investment_cases:legal_review` |
| `InvestmentCases_CeoApprove` | `investment_cases:ceo_approve` |
| `GuaranteeCases_Read` | `guarantee_cases:read` |
| `GuaranteeCases_Write` | `guarantee_cases:write` |
| `GuaranteeCases_CreditReview` | `guarantee_cases:credit_review` |
| `GuaranteeCases_LegalReview` | `guarantee_cases:legal_review` |
| `GuaranteeCases_FinanceReview` | `guarantee_cases:finance_review` |
| `GuaranteeCases_CeoApprove` | `guarantee_cases:ceo_approve` |
| `GuaranteeCases_SetApplicantCreditLimit` | `guarantee_cases:set_applicant_credit_limit` |

**نقش → API:** `RolePermissions.RolePermissionMappings` در همان فایل. `Admin` = `AllPermissions`.

---

### لایه ۲ — سرمایه‌گذاری (`CasePermissions.cs`)

| ثابت | رشته |
|------|------|
| `Create` | `cases:create` |
| `ReadOwn` | `cases:read_own` |
| `ReadAll` | `cases:read_all` |
| `ViewInternalComments` | `cases:view_internal_comments` |
| `CreateInternalComment` | `cases:create_internal_comment` |
| `ViewEvaluations` | `cases:view_evaluations` |
| `UpsertEvaluations` | `cases:upsert_evaluations` |
| `ManageValuations` | `cases:manage_valuations` |
| `ManageContracts` | `cases:manage_contracts` |
| `ManageFinancialWorksheet` | `cases:manage_financial_worksheet` |
| `ManagePayments` | `cases:manage_payments` |
| `CeoApprove` | `cases:ceo_approve` |
| `UploadDocuments` | `cases:upload_documents` |
| `DownloadDocuments` | `cases:download_documents` |
| `UploadCommentAttachments` | `cases:upload_comment_attachments` |

**نقش‌های دارای ورودی در `CaseAuthorizationService`:**  
`Applicant`, `InvestmentExpert`, `InvestmentManager`, `LegalExpert`, `LegalManager`, `FinancialExpert`, `FinancialManager`, `TechnicalExpert`, `TechnicalManager`, `Ceo`.  
(`Admin` در HasPermission bypass — نیاز به ورودی در دیکشنری ندارد.)

**نکته:** `ReadAll` برای هر `IsInternalUser` true است، حتی اگر در آرایه نقش نباشد.

---

### لایه ۳ — ضمانت (`GuaranteePermissions.cs`)

| ثابت | رشته |
|------|------|
| `Create` | `guarantee_cases:create` |
| `ReadOwn` | `guarantee_cases:read_own` |
| `ReadAll` | `guarantee_cases:read_all` |
| `ViewInternalComments` | `guarantee_cases:view_internal_comments` |
| `CreateInternalComment` | `guarantee_cases:create_internal_comment` |
| `ManageApprovalForm` | `guarantee_cases:manage_approval_form` |
| `ManageContracts` | `guarantee_cases:manage_contracts` |
| `ManageAttachments` | `guarantee_cases:manage_attachments` |
| `ManageIssuance` | `guarantee_cases:manage_issuance` |
| `CeoApprove` | `guarantee_cases:ceo_approve` |
| `SetApplicantCreditLimit` | `guarantee_cases:set_applicant_credit_limit` |
| `UploadDocuments` | `guarantee_cases:upload_documents` |
| `DownloadDocuments` | `guarantee_cases:download_documents` |

**نقش‌های دارای ورودی:** `Applicant`, `CreditExpert`, `CreditManager`, `LegalExpert`, `LegalManager`, `FinancialExpert`, `FinancialManager`, `Ceo`, `Admin`, (و در صورت تنظیم تست: `TechnicalExpert`).

---

### نقش‌های سیستم (`UserRole`)

| Enum | Claim / JWT | یادداشت |
|------|-------------|---------|
| `Applicant` | `Applicant` | متقاضی |
| `InvestmentExpert` | `InvestmentExpert` | alias: `InvestmentUnit` |
| `InvestmentManager` | `InvestmentManager` | |
| `Ceo` | `Ceo` | alias JWT: `CEO` |
| `LegalExpert` / `LegalManager` | همان نام | alias: `LegalUnit` → Expert |
| `FinancialExpert` / `FinancialManager` | همان نام | alias: `FinancialUnit` |
| `TechnicalExpert` / `TechnicalManager` | همان نام | |
| `CreditExpert` / `CreditManager` | همان نام | ماژول ضمانت |
| `Admin` | `Admin` | همه لایه‌ها |

---

## 12. سیاست‌های ASP.NET (Policy)

ثبت در `Core.API/DependencyInjection/AuthorizationServiceCollectionExtensions.cs` (`AddCoreAuthorization`):

| Policy | نوع بررسی | معنی |
|--------|-----------|------|
| `AdminOnly` | نقش | فقط `Admin` |
| `ApplicantOnly` | نقش | `Applicant` یا `Admin` |
| `InternalOnly` | نقش | لیست نقش‌های داخلی + `CEO` |
| `GuaranteeCases.CreditReview` | permission | `guarantee_cases:credit_review` |
| `GuaranteeCases.CeoApprove` | نقش | `Ceo`, `Admin`, `CEO` |
| `GuaranteeCases.CeoOnly` | نقش | `Ceo`, `CEO` |
| `InvestmentCases.Review` | permission | `investment_cases:review` |
| `InvestmentCases.FinanceReview` | permission | `investment_cases:finance_review` |
| `InvestmentCases.LegalReview` | permission | `investment_cases:legal_review` |
| `InvestmentCases.CeoApprove` | نقش | `Ceo`, `Admin`, `CEO` |
| `Dashboard.Ceo` | نقش | داشبورد CEO |
| `Dashboard.Board` | نقش | هیئت / مدیر سرمایه‌گذاری |

`PermissionAuthorizationHandler`:

1. نقش `Admin` → allow
2. اگر permission در `RolePermissions.RolePermissionMappings` برای claim نقش کاربر باشد → allow
3. وگرنه `IIdentityClient.ValidateUserPermissionAsync` (کش + `PermissionService`)
4. استثنا CEO برای برخی permissionهای تأیید

**مهم:** دادن `investment_cases:ceo_approve` در `Permissions.cs` به یک نقش، **کافی نیست** اگر endpoint فقط `InvestmentCases.CeoApprove` (نقش-based) دارد.

---

## 13. کش Permission، JWT و عیب‌یابی

### کش

| مورد | مقدار |
|------|--------|
| سرویس | `DistributedPermissionCacheService` |
| کلید | `permissions:user:{userId}` |
| TTL | ۳۰ دقیقه (`PermissionService`) |
| پاک‌سازی | `IPermissionCacheService.RemoveUserPermissionsAsync(userId)` — **فعلاً بعد از Update کاربر صدا زده نمی‌شود** |

**Redis (دستی):**

```bash
# نمونه — userId را از جدول User بگیرید
DEL permissions:user:xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

### JWT

- نقش در claim `Role` / `ClaimTypes.Role` است
- تا expire یا login مجدد، **نقش قدیمی** در توکن می‌ماند
- `IUserContext.Roles` از همان JWT می‌خواند

### عیب‌یابی سریع

| علامت | احتمال | کار |
|-------|--------|-----|
| 403 روی Controller | لایه ۱ یا Policy نقش-based | `Permissions.cs` + Policy در بخش ۱۲ |
| 200 ولی پیام «دسترسی ندارید» در body | لایه ۲ یا ۳ | `CaseAuthorizationService` / `GuaranteeAuthorizationService` |
| Admin کار می‌کند، نقش دیگر نه | فقط mapping آن نقش | سه فایل mapping |
| بعد از deploy هنوز رفتار قدیمی | کش / JWT | re-login + DEL کش |
| LegalUnit در JWT | Normalize به LegalExpert | mapping زیر `LegalExpert` |

### آینده (اختیاری — یک فایل متمرکز)

اگر تیم زیاد permission عوض می‌کند، می‌توان `AuthorizationRegistry.cs` ساخت که هر سه لایه را export کند و سه سرویس فقط از آن بخوانند — فعلاً در کد پیاده نشده؛ تا آن زمان **همین بخش ۹–۱۳** مرجع است.

---

## 14. ریپازیتوری و دیتابیس

### الگوی کلی

```
I{X}Repository  (Core.Application)
    ↑
{X}Repository   (Core.Infrastructure) — معمولاً CoreDbContext را inject می‌کند
```

- Repositoryهای ساده از `EfRepository<TEntity, TKey>` ارث می‌برند (`CompanyRepository`)
- Repositoryهای سنگین مثل `InvestmentCaseRepository` متدهای اختصاصی با `Include` / `AsSplitQuery` / فیلتر scoped دارند

### `ICoreUnitOfWork`

```csharp
public interface ICoreUnitOfWork
{
    IUserRepository Users { get; }
    ICompanyRepository Companies { get; }
    IInvestmentCaseRepository InvestmentCases { get; }
    // ...
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

`UserService` و برخی flowها از UoW استفاده می‌کنند؛ `InvestmentCaseAppService` اغلب مستقیم `IInvestmentCaseRepository` + `ICoreDbContext` یا UoW.

### `ICoreDbContext`

برای دسترسی مستقیم `DbSet` در AppService وقتی query کوچک است یا چند aggregate در یک transaction.  
**ترجیح:** برای queryهای پیچیده و قابل استفاده مجدد، متد جدید روی Repository بگذارید.

### فیلتر Scoped (امنیت داده پرونده)

`InvestmentCaseRepository.ApplyScopedFilter`:

- کاربر داخلی (`IsInternalUser`) → همه پرونده‌ها
- متقاضی → فقط `ApplicantUserId == userId`

همیشه برای get/search از متدهای `GetScoped*` / `SearchScoped*` استفاده شود مگر Admin صریح.

### اسکیمای PostgreSQL

| Schema | جداول نمونه |
|--------|-------------|
| `Identity` | `User`, `Company`, `RefreshToken`, `UserSession` |
| `Cases` | `investment_cases`, `case_documents`, `payment_records`, … |

Configuration: `Core.Persistence/Configurations/**/*.cs` با `builder.ToTable(..., DbSchemas.X)`.

---

## 15. چک‌لیست: اضافه کردن Entity جدید

فرض: aggregate جدید `RiskAssessment` وابسته به پرونده.

### 1. Domain
- `Core.Domain/Entities/RiskAssessment.cs` — ارث از `Entity<Guid>` یا `AuditableEntity` طبق نیاز
- رابطه با `InvestmentCase` در همان entity یا navigation
- invariantها در متدهای domain (نه setter آزاد برای همه فیلدها)

### 2. Persistence — Configuration
- `Core.Persistence/Configurations/RiskAssessmentConfiguration.cs`
- `IEntityTypeConfiguration<RiskAssessment>`
- جدول، کلید، FK، index، `HasMaxLength`, schema `DbSchemas.Cases`

### 3. DbContext
- `CoreDbContext`: `DbSet<RiskAssessment> RiskAssessments => Set<RiskAssessment>();`
- `ICoreDbContext`: همان property

### 4. Migration
- دستور در [بخش 15](#15-migration-ef-core)

### 5. Repository (توصیه برای aggregate غیر trivial)
- `IRiskAssessmentRepository` در `Core.Application/Abstractions/`
- `RiskAssessmentRepository` در `Core.Infrastructure/Persistence/`
- ثبت در `ServiceCollectionExtensions.AddCoreInfrastructure`:
  `services.AddScoped<IRiskAssessmentRepository, RiskAssessmentRepository>();`
- اگر لازم است: property روی `ICoreUnitOfWork` + `CoreUnitOfWork`

### 6. Application
- DTO / Request / Response
- Validator با FluentValidation (`AddValidatorsFromAssemblyContaining<...>`)
- متد در `InvestmentCaseAppService` یا سرویس جدا
- `CaseAuthorizationService` در صورت نیاز permission جدید

### 7. API
- action در `InvestmentCasesController` با route مناسب
- `[Authorize]` / policy

### 8. Mapster
- `ApplicationMapsterConfig.cs` در صورت mapping غیر پیش‌فرض

### نمونه Repository ساده (مثل Company)

```csharp
// Application
public interface ICompanyRepository : IWriteRepository<Company, Guid>
{
    Task<List<Company>> GetOwnedByUserAsync(Guid ownerUserId, CancellationToken ct = default);
}

// Infrastructure
public sealed class CompanyRepository(CoreDbContext dbContext)
    : EfRepository<Company, Guid>(dbContext, dbContext.Companies), ICompanyRepository
{
    public Task<List<Company>> GetOwnedByUserAsync(Guid ownerUserId, CancellationToken ct = default)
        => ListAsync(c => c.OwnerUserId == ownerUserId, asNoTracking: true, cancellationToken: ct);
}
```

---

## 16. Migration EF Core

پروژه startup: **Core.API**  
پروژه DbContext: **Core.Persistence**

از ریشه ریپو (PowerShell):

```powershell
dotnet ef migrations add AddRiskAssessment `
  --project src/Services/CoreService/Core.Persistence/Core.Persistence.csproj `
  --startup-project src/Services/CoreService/Core.API/Core.API.csproj `
  --context CoreDbContext

dotnet ef database update `
  --project src/Services/CoreService/Core.Persistence/Core.Persistence.csproj `
  --startup-project src/Services/CoreService/Core.API/Core.API.csproj
```

**نکات:**
- Connection string از `appsettings` یا environment خوانده می‌شود (`ConnectionStrings:Postgres`)
- قبل از migration روی production، backup بگیرید
- `InvestmentCase` قبلاً `xmin/RowVersion` داشت — در `OnModelCreating` حذف شده؛ migration جدید تکرار نکند

---

## 17. Unit of Work و SaveChanges

- `SaveChangesAsync` رویدادهای دامنه را از طریق `DbContextBase` dispatch می‌کند (`IDomainEventDispatcher` / MediatR)
- Interceptorها: Audit (`CreatedAt`/`UpdatedAt`), SoftDelete, InvestmentCase suppressor
- برای transition پرونده گاهی `ExecuteUpdate` مستقیم روی جدول استفاده می‌شود تا concurrency روی `InvestmentCase` نشکند (در `InvestmentCaseAppService` جستجو کنید)

---

## 18. Workflow (Elsa) و پرونده سرمایه‌گذاری

- تعریف workflow: `Core.Workflow/Workflows/InvestmentCaseWorkflow.cs`
- Orchestrator: `ElsaCaseWorkflowOrchestrator` — `ICaseWorkflowOrchestrator`
- در Development ممکن است persistence Elsa in-memory باشد؛ در Production از همان Postgres استفاده می‌کند (`AddCoreWorkflow`)
- `InvestmentCase.WorkflowInstanceId` لینک به instance Elsa

تغییر مراحل workflow = ویرایش workflow + `CaseStateManager` + احتمالاً enum `CaseStatus` / `CasePhase` + داک فرانت.

---

## 19. App Service، Validator، DTO و Mapster

| موضوع | محل |
|-------|-----|
| Use case اصلی پرونده | `InvestmentCaseAppService.cs` (بزرگ — قبل از refactor بخش مربوطه را پیدا کنید) |
| Validator درخواست | `Core.Application/Validation/*.cs` |
| DTO پرونده | `Core.Application/DTOs/` |
| DTO کاربر | `Core.Application/Identity/DTOs/` |
| Mapping | `ApplicationMapsterConfig.cs` + `ICaseDtoMapper` |

اعتبارسنجی خودکار: `AddFluentValidationAutoValidation()` در `Program.cs`.

---

## 20. Controller و پاسخ API

- پایه: `ApiControllerBase`
- موفقیت پرونده: اغلب `Respond(result, CaseSuccessMessages.*, HttpStatusCode.Accepted)` برای transition
- body خالی برای submit: `ReadTransitionRequestAsync` — POST بدون body مشکلی ندارد

Versioning: attribute `[ApiVersion(1.0)]` + URL segment `v{version}`.

---

## 21. پیکربندی (appsettings)

فایل: `Core.API/appsettings.json` (+ `appsettings.Development.json` در صورت وجود)

| کلید | کاربرد |
|------|--------|
| `ConnectionStrings:Postgres` | EF Core |
| `ConnectionStrings:Redis` | OTP، Session، Permission cache — اگر خالی باشد MemoryCache |
| `JwtKey` / `ENCKey` | امضا و رمزنگاری JWT |
| `Otp:*` | TTL، DevBypass، rate limit |
| `Session:*` | انقضای نشست |
| `Sms:*` | کاوه‌نگار، صف، Mongo audit |
| `LiaraStorage:*` | S3-compatible برای اسناد |
| `RefreshTokenPepper` | هش refresh token |
| `Serilog` | Console / Seq |

**امنیت:** secrets را commit نکنید؛ در محیط واقعی از env var یا secret manager استفاده شود.

متغیر محیطی اختیاری URL: `CORE_URLS` یا `ASPNETCORE_URLS`.

---

## 22. لاگ، Observability و خطاها

- `ApplicationLog.*` در سرویس‌های Application
- OpenTelemetry: service name `core-service` در `Program.cs`
- CorrelationId middleware از BuildingBlocks

پیام‌های کاربر (فارسی): `ApiMessages`, `CaseSuccessMessages`, `IdentityMessages` در Application/Common.

---

## 23. کارهای رایج نگه‌داری — چک‌لیست سریع

| کار | کجا |
|-----|-----|
| Endpoint جدید پرونده | `InvestmentCasesController` + `InvestmentCaseAppService` + Validator |
| محدودیت نقش روی endpoint | `AuthorizationServiceCollectionExtensions.cs` + `Permissions.cs` — **بخش ۹–۱۳** |
| منع عملیات سرمایه‌گذاری | `CaseAuthorizationService` + `CasePermissions` |
| منع عملیات ضمانت | `GuaranteeAuthorizationService` + `GuaranteePermissions` |
| نقش جدید | `UserRole` + سه فایل mapping + Policy + `IsInternalUser` — **بخش ۱۰-D** |
| Permission API جدید | `Permissions.cs` + `AllPermissions` + Policy |
| Permission پرونده جدید | `CasePermissions` + `CaseAuthorizationService` |
| Permission ضمانت جدید | `GuaranteePermissions` + `GuaranteeAuthorizationService` |
| **فقط** یک permission به نقش | **بخش ۱۰-A** — معمولاً ۲–۳ فایل |
| جدول DB جدید | Entity + Configuration + DbContext + Migration |
| Query سنگین | `InvestmentCaseRepository` متد جدید با `AsSplitQuery` |
| مسیر API عوض شد | Swagger metadata + `docs/frontend` + `Frontend/app.js` |
| تست دستی سریع | `Frontend/` test panel + Swagger |
| بعد از تغییر نقش کاربر | Login مجدد + پاک کردن `permissions:user:{id}` در Redis |

### Build و اجرا

```powershell
dotnet build src/Services/CoreService/Core.API/Core.API.csproj
dotnet run --project src/Services/CoreService/Core.API/Core.API.csproj
```

---

## 24. فایل‌های مرجع (Index)

| موضوع | مسیر |
|-------|------|
| نقش و claim | `Core.Domain/Identity/UserRole.cs` |
| **راهنمای کامل دسترسی (شروع از اینجا)** | همین سند — **بخش ۹ تا ۱۳** |
| Permission API + نقش | `Core.Application/Identity/Authorization/Permissions.cs` |
| Permission سرمایه‌گذاری | `CasePermissions.cs`, `CaseAuthorizationService.cs` |
| Permission ضمانت | `GuaranteePermissions.cs`, `GuaranteeAuthorizationService.cs` |
| Policyهای HTTP | `Core.API/DependencyInjection/AuthorizationServiceCollectionExtensions.cs` |
| Handler سیاست | `Core.API/Authorization/PermissionAuthorizationHandler.cs` |
| DI اصلی API | `Core.API/Program.cs` |
| DI Infrastructure | `Core.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` |
| DI Persistence | `Core.Persistence/DependencyInjection/ServiceCollectionExtensions.cs` |
| DbContext | `Core.Persistence/CoreDbContext.cs` |
| JWT | `Core.Infrastructure/Identity/Identity/TokenHandler/TokenHelper.cs` |
| کش permission | `Core.Infrastructure/Identity/Services/Authorization/DistributedPermissionCacheService.cs` |
| State machine پرونده | `Core.Application/Services/CaseStateManager.cs` |
| Repository پرونده | `Core.Infrastructure/Persistence/InvestmentCaseRepository.cs` |
| راهنمای فرانت API | `docs/frontend/INVESTMENT_CASE_API_GUIDE.md` |
| راهنمای ضمانت‌نامه | `docs/frontend/GUARANTEE_CASE_API_GUIDE.md` |
| تمدید ضمانت‌نامه | `docs/frontend/GUARANTEE_RENEWAL_API_GUIDE.md` |
| State machine ضمانت‌نامه | `Core.Application/Services/GuaranteeCaseStateManager.cs` |
| کنترلر ضمانت‌نامه | `Core.API/Controllers/GuaranteeCasesController.cs` |
| کارتابل یکپارچه | `Core.API/Controllers/KanbanController.cs` |

---

## ماژول ضمانت‌نامه (Guarantee)

- Aggregate: `GuaranteeCase` + `GuaranteeCaseApplication`, `GuaranteeApprovalForm`, `GuaranteeCaseDocument`, `GuaranteeRenewalCase`
- جداول در schema `Cases` با پیشوند `guarantee_*`
- نقش‌های جدید: `CreditExpert` (50), `CreditManager` (51)
- Migration: `AddGuaranteeModule`
- API: `/api/v1/guaranteecases`, `/api/v1/guarantee-renewals`, `/api/v1/kanban`
- الگوی transition/comment/document مشابه سرمایه‌گذاری؛ بدون duplicate داده Company/User روی application

---

### جمع‌بندی برای شروع سریع

1. Solution را از `Core.API` اجرا کنید و Swagger را باز کنید.  
2. یک بار OTP/Dev login را در `Frontend` یا Swagger امتحان کنید تا JWT بگیرید.  
3. برای هر تغییر دسترسی، **بخش ۹–۱۳** را باز کنید — معمولاً `Permissions.cs` + `CaseAuthorizationService` + `GuaranteeAuthorizationService` (نه فقط یک فایل).  
4. Entity جدید = Domain → Configuration → DbContext → Migration → Repository → AppService → Controller.  
5. Claim نقش فقط از `ClaimTypes.Role` در JWT می‌آید — `IUserContext.Roles` همان را می‌خواند.

اگر بخشی از سیستم (مثلاً Dashboard یا SMS) را عمیق‌تر مستند کنیم، همان ماژول را به‌صورت فصل جدا در همین پوشه `docs/backend/` اضافه کنید.
