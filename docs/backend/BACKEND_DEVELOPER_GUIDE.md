# راهنمای توسعه‌دهنده بک‌اند — Financial-Core

**مخاطب:** برنامه‌نویس بک‌اند تازه‌وارد که قرار است این ریپو را نگه‌داری و توسعه دهد.  
**هدف:** جایگزین «گشتن در کل پروژه» — مسیرها، قراردادها، و چک‌لیست‌های عملی برای تغییرات روزمره.  
**آخرین هم‌راستاسازی با کد:** بهار ۱۴۰۵ (نسخه API v1، مسیرهای `investmentcases` و `identity`).

> برای قرارداد HTTP و فلو پرونده سرمایه‌گذاری از نگاه فرانت، ببینید: [`docs/frontend/INVESTMENT_CASE_API_GUIDE.md`](../frontend/INVESTMENT_CASE_API_GUIDE.md).

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

## 9. مجوزدهی — دو لایه جدا

**اشتباه رایج:** فکر کردن `investment_cases:review` همان `cases:manage_valuations` است. در این پروژه **دو سیستم موازی** داریم:

### لایه A — Permissionهای API (`Permissions` + `RolePermissions`)

- **فایل:** `Core.Application/Identity/Authorization/Permissions.cs`
- **فرمت:** `resource:action` مثل `users:read`, `investment_cases:finance_review`
- **نگاشت نقش → لیست permission:** `RolePermissions.RolePermissionMappings`
- **بررسی:** 
  - `PermissionService.HasPermissionAsync` (با کش Redis)
  - `PermissionAuthorizationHandler` روی `[Authorize(Policy = "...")]` اگر `PermissionRequirement` باشد
- **Admin:** `UserRole.Admin` در `PermissionService` همیشه `true`

### لایه B — Permissionهای دامنه پرونده (`CasePermissions` + `CaseAuthorizationService`)

- **فایل:** `Core.Application/Authorization/CasePermissions.cs`
- **فرمت:** `cases:read_own`, `cases:manage_payments`, …
- **نگاشت:** داخل `CaseAuthorizationService` — دیکشنری `RolePermissions` جدا از `RolePermissions` کلاس Identity
- **بررسی:** `ICaseAuthorizationService.HasPermission(...)` در `InvestmentCaseAppService` و سرویس‌های مرتبط
- **Admin:** نقش `Admin` در JWT → همه permissionهای پرونده

### چه موقع کدام را عوض کنید؟

| می‌خواهید… | ویرایش کنید |
|------------|-------------|
| Endpoint جدید با `[Authorize(Policy = "InvestmentCases.X")]` | `Permissions.cs` + `RolePermissions` + `Program.cs` policy |
| منع/اجازه عملیات داخل AppService (مثلاً ثبت پرداخت) | `CasePermissions` + `CaseAuthorizationService` |
| فقط نقش ASP.NET (`ApplicantOnly`, `InternalOnly`) | `Program.cs` — لیست `RequireRole` |

---

## 10. نقش (Role) — اضافه، تغییر، حذف دسترسی

### مدل نقش در دیتابیس

- Enum: `Core.Domain.Identity.UserRole` (مقادیر عددی ثابت — **به مقادیر موجود دست نزنید**؛ فقط مقدار جدید اضافه کنید)
- روی `User.Role` ذخیره می‌شود
- ثابت‌های claim: `UserRoleClaims` در همان فایل `UserRole.cs`

### چک‌لیست: اضافه کردن نقش جدید

1. **Enum** — مقدار جدید با عدد آزاد (مثلاً `RiskManager = 50`) در `UserRole.cs`
2. **UserRoleClaims** — `public const string RiskManager = nameof(UserRole.RiskManager);`
3. **RolePermissions** (`Permissions.cs`) — ورودی در `RolePermissionMappings` با آرایه permissionهای API
4. **CaseAuthorizationService** — اگر به پرونده دسترسی دارد، آرایه `CasePermissions.*` مناسب
5. **Program.cs** — اگر internal است: اضافه به policy `InternalOnly`؛ policy اختصاصی در صورت نیاز
6. **CaseAuthorizationService.IsInternalUser** — اگر نقش داخلی است، به لیست اضافه شود
7. **فرانت / seed** — `Frontend/config.js` personas، داک فرانت
8. **Migration لازم نیست** اگر فقط enum است (ستون int همان است)

### دادن دسترسی API به یک نقش موجود

فایل: `Permissions.cs` → `RolePermissions.RolePermissionMappings`

```csharp
[UserRoleClaims.InvestmentManager] =
[
    Permissions.Users_Read,
    // ...
    Permissions.InvestmentCases_FinanceReview  // ← اضافه
],
```

اگر permission **جدید** است:

1. ثابت در کلاس `Permissions` تعریف کنید: `public const string X = "module:action";`
2. به `AllPermissions` اضافه کنید (برای Admin)
3. به نقش‌های هدف در `RolePermissionMappings` اضافه کنید
4. در Controller: `[Authorize(Policy = "...")]` یا `PermissionRequirement` بسازید

### گرفتن دسترسی از نقش

همان آرایه را ویرایش کنید — permission را از لیست نقش حذف کنید.  
کاربران آن نقش بعد از **پاک شدن کش** (بخش 12) یا انقضای ۳۰ دقیقه‌ای کش، اثر می‌گیرند.

### تغییر نقش یک کاربر

- API: `PUT /identity/users/{id}` با `UpdateUserDto.Role` — **فقط Admin** (`UserService` چک می‌کند)
- JWT قبلی تا expire معتبر است؛ نقش در توکن **به‌روز نمی‌شود** تا login/refresh مجدد
- کش permission کاربر را invalidate کنید (پایین)

### Permission سطح پرونده به نقش

فایل: `CaseAuthorizationService.cs` — دیکشنری `RolePermissions` (توجه: نام کلاس داخلی است، با `RolePermissions` در `Permissions.cs` اشتباه گرفته نشود)

مثال: اجازه `CasePermissions.ManagePayments` به `FinancialExpert`:

```csharp
private static readonly string[] FinancialUnitPermissions =
[
    CasePermissions.ReadAll,
  // ...
    CasePermissions.ManagePayments,  // اضافه
];
```

---

## 11. سیاست‌های ASP.NET در Program.cs

ثبت در `builder.Services.AddAuthorization`:

| Policy | معنی تقریبی |
|--------|-------------|
| `ApplicantOnly` | `Applicant` یا `Admin` |
| `InternalOnly` | همه نقش‌های کارشناسی/مدیریتی + CEO + Admin |
| `InvestmentCases.Review` | permission `investment_cases:review` |
| `InvestmentCases.FinanceReview` | `investment_cases:finance_review` |
| `InvestmentCases.LegalReview` | `investment_cases:legal_review` |
| `InvestmentCases.CeoApprove` | نقش `CEO` / `Ceo` / `Admin` (عمداً role-based به‌خاطر lag کش) |
| `Dashboard.Ceo` / `Dashboard.Board` | داشبورد ejecutive |

`PermissionAuthorizationHandler`:

1. اول از claim نقش (`RolePermissions`) سریع جواب می‌دهد
2. اگر نشد، `IIdentityClient.ValidateUserPermissionAsync` (همان منطق `PermissionService`)

**CEO:** برای `investment_cases:ceo_approve` handler استثنا دارد — نقش `CEO`/`Ceo`/`Admin` کافی است.

---

## 12. کش Permission و نکته مهم بعد از تغییر نقش

- سرویس: `DistributedPermissionCacheService`
- کلید Redis/Memory: `permissions:user:{userId}`
- TTL پیش‌فرض: **۳۰ دقیقه** (`PermissionService.GetUserPermissionsAsync`)

متد پاک‌سازی وجود دارد: `IPermissionCacheService.RemoveUserPermissionsAsync(userId)`  
**ولی در `UserService.Update` فعلاً فراخوانی نمی‌شود** — اگر نقش کاربر را عوض کردید و رفتار عجیب دیدید:

- یا منتظر expire کش بمانید
- یا Redis key را دستی حذف کنید
- یا در همان PR تغییر نقش، `RemoveUserPermissionsAsync` را بعد از `Update` صدا بزنید (بهبود پیشنهادی)

**JWT:** حتی با کش درست، claim نقش در توکن قدیمی است — برای تست سیاست‌های role-based حتماً **دوباره login** کنید.

---

## 13. ریپازیتوری و دیتابیس

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

## 14. چک‌لیست: اضافه کردن Entity جدید

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

## 15. Migration EF Core

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

## 16. Unit of Work و SaveChanges

- `SaveChangesAsync` رویدادهای دامنه را از طریق `DbContextBase` dispatch می‌کند (`IDomainEventDispatcher` / MediatR)
- Interceptorها: Audit (`CreatedAt`/`UpdatedAt`), SoftDelete, InvestmentCase suppressor
- برای transition پرونده گاهی `ExecuteUpdate` مستقیم روی جدول استفاده می‌شود تا concurrency روی `InvestmentCase` نشکند (در `InvestmentCaseAppService` جستجو کنید)

---

## 17. Workflow (Elsa) و پرونده سرمایه‌گذاری

- تعریف workflow: `Core.Workflow/Workflows/InvestmentCaseWorkflow.cs`
- Orchestrator: `ElsaCaseWorkflowOrchestrator` — `ICaseWorkflowOrchestrator`
- در Development ممکن است persistence Elsa in-memory باشد؛ در Production از همان Postgres استفاده می‌کند (`AddCoreWorkflow`)
- `InvestmentCase.WorkflowInstanceId` لینک به instance Elsa

تغییر مراحل workflow = ویرایش workflow + `CaseStateManager` + احتمالاً enum `CaseStatus` / `CasePhase` + داک فرانت.

---

## 18. App Service، Validator، DTO و Mapster

| موضوع | محل |
|-------|-----|
| Use case اصلی پرونده | `InvestmentCaseAppService.cs` (بزرگ — قبل از refactor بخش مربوطه را پیدا کنید) |
| Validator درخواست | `Core.Application/Validation/*.cs` |
| DTO پرونده | `Core.Application/DTOs/` |
| DTO کاربر | `Core.Application/Identity/DTOs/` |
| Mapping | `ApplicationMapsterConfig.cs` + `ICaseDtoMapper` |

اعتبارسنجی خودکار: `AddFluentValidationAutoValidation()` در `Program.cs`.

---

## 19. Controller و پاسخ API

- پایه: `ApiControllerBase`
- موفقیت پرونده: اغلب `Respond(result, CaseSuccessMessages.*, HttpStatusCode.Accepted)` برای transition
- body خالی برای submit: `ReadTransitionRequestAsync` — POST بدون body مشکلی ندارد

Versioning: attribute `[ApiVersion(1.0)]` + URL segment `v{version}`.

---

## 20. پیکربندی (appsettings)

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

## 21. لاگ، Observability و خطاها

- `ApplicationLog.*` در سرویس‌های Application
- OpenTelemetry: service name `core-service` در `Program.cs`
- CorrelationId middleware از BuildingBlocks

پیام‌های کاربر (فارسی): `ApiMessages`, `CaseSuccessMessages`, `IdentityMessages` در Application/Common.

---

## 22. کارهای رایج نگه‌داری — چک‌لیست سریع

| کار | کجا |
|-----|-----|
| Endpoint جدید پرونده | `InvestmentCasesController` + `InvestmentCaseAppService` + Validator |
| محدودیت نقش روی endpoint | `[Authorize(Policy = "...")]` در `Program.cs` + `Permissions.cs` |
| منع عملیات داخل سرویس | `CaseAuthorizationService` + `Ensure…` در AppService |
| نقش جدید | `UserRole` + `UserRoleClaims` + هر دو `RolePermissions` + `Program.cs` + `IsInternalUser` |
| Permission API جدید | `Permissions` + `AllPermissions` + `RolePermissionMappings` |
| Permission پرونده جدید | `CasePermissions` + `CaseAuthorizationService` |
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

## 23. فایل‌های مرجع (Index)

| موضوع | مسیر |
|-------|------|
| نقش و claim | `Core.Domain/Identity/UserRole.cs` |
| Permission API + نقش | `Core.Application/Identity/Authorization/Permissions.cs` |
| Permission پرونده | `Core.Application/Authorization/CasePermissions.cs`, `CaseAuthorizationService.cs` |
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
3. برای هر تغییر دسترسی، **هر دو** `Permissions.cs` (API) و `CaseAuthorizationService` (پرونده) را چک کنید.  
4. Entity جدید = Domain → Configuration → DbContext → Migration → Repository → AppService → Controller.  
5. Claim نقش فقط از `ClaimTypes.Role` در JWT می‌آید — `IUserContext.Roles` همان را می‌خواند.

اگر بخشی از سیستم (مثلاً Dashboard یا SMS) را عمیق‌تر مستند کنیم، همان ماژول را به‌صورت فصل جدا در همین پوشه `docs/backend/` اضافه کنید.
