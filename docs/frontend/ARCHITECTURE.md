# Frontend architecture

Vanilla JS panel: no router, no build step. Navigation is tab-based DOM show/hide plus module-specific subtabs.

## Bootstrap sequence

`index.html` loads scripts in dependency order, then `app.js` on `DOMContentLoaded`:

1. `config.js` → `TESTPANEL_CONFIG`
2. Workflow models (`workflow-model.js`, `guarantee-workflow-model.js`, `loan-workflow-model.js`)
3. `ui-components.js`
4. Feature modules (`kanban`, portals, cases-hub, dashboards, admin)
5. `app.js` → builds `TestPanel`, calls each `init*(TestPanel)`

```634:653:Frontend/index.html
    <script src="config.js"></script>
    <script src="js/workflow-model.js"></script>
    ...
    <script src="app.js"></script>
```

## Layering

```
index.html (structure, tab shells)
    app.js (HTTP client, auth, session/case state, tab router)
    js/* (feature modules — each owns a tab or portal region)
    workflow-*-model.js (status/role metadata, no HTTP)
    ui-components.js / dashboard-ui.js (shared render helpers)
```

Modules are IIFEs that attach to `window` (`initPortal`, `CasesHub`, etc.). They do not import each other; shared deps are globals.

## Tab routing

`app.js` → `wireTabs()` toggles `.navbtn.is-active` and matching `#tab*`.is-active`. Side effects on enter:

- `tabInbox` → `kanbanRefresh()`
- `tabCases` → `CasesHub.loadCases()`
- `tabDashboard` → `refreshFundCreditLimitsAccess()`

No URL hash routing; refresh returns to default tab state.

## Cases flow

`cases-hub.js` is the shell for all three case types.

```
Cases list (module tab: investment | guarantee | loan)
    → openCase(module, id)
    → detail view + subtab (workflow | attachments | history)
    → shows one portal root (#investmentPortalRoot | #guaranteePortalRoot | #loanPortalRoot)
```

| Module | API base helper | Portal init | Workflow model |
|--------|-----------------|-------------|----------------|
| investment | `TestPanel.casesBasePath()` | `initPortal` → `portal.js` | `WorkflowModel` |
| guarantee | `TestPanel.guaranteeCasesBasePath()` | `initGuaranteePortal` | `GuaranteeWorkflowModel` |
| loan | `TestPanel.loanCasesBasePath()` | `initLoanPortal` | `LoanWorkflowModel` |

Portals listen for `testpanel:case-changed` and reload when the active case id matches their module.

## Workflow models

Each `*-workflow-model.js` defines:

- `STEPS` / status codes and Persian titles
- `PHASES`, organizational `UNITS` (investment only)
- `normalizeRole(roleText, roleNumber)` — shared role resolution
- `actionsForStatus(status, role)` or equivalent — drives action buttons in portals

Portals map actions to `panel.apiRequest({ method, path: casesPath + suffix, body })`. Paths are relative to the module base (e.g. `/data-entry1/submit`).

## Kanban (inbox)

`kanban.js` calls `GET {kanbanBasePath}/action-required` and renders two columns:

- **منتظر اقدام من** — items requiring user action
- **پیگیری** — watch list

Card click → `CasesHub.openCase(module, caseId)` (module from API `module` field: 1 investment, 2 guarantee, 3 renewal, 4 loan).

## Dashboards

### Home (`tabDashboard`)

`home-dashboard.js`:

- Default: `GET /api/v1/dashboard/me`
- Admin role switcher: per-role endpoints (`/dashboard/ceo`, `/dashboard/department?departmentKey=...`, etc.)
- Sub-tab **عملکرد پرسنل / KPI** → `employee-kpi-dashboard.js` → `/api/v1/analytics/employee-kpis`
- CEO block **سقف اعتبار دوره‌ای** → `fund-credit-limits.js` → `/api/v{casesVersion}/fund-credit-limits`

Rendering delegated to `dashboard-ui.js` + Chart.js.

### Admin dashboard (`tabAdminDashboard`)

`admin-dashboard.js` loads `GET /api/v1/dashboard/admin-overview`, renders KPI strip, health, module sections, and role sections (executive, CEO, board, departments, applicant) with dedicated chart canvases.

## Admin management

| Module | Access roles | API root |
|--------|--------------|----------|
| `admin-users.js` | CEO, TechnicalExpert, Admin | `/api/v1/identity/users` |
| `admin-companies.js` | CEO, TechnicalExpert, Admin | `/api/v1/identity/companies` |

Both use paginated list + inline edit forms; visibility toggled via `refresh*Access` on session change.

## Shared UI (`ui-components.js`)

Key exports on `window.UIComponents`:

- `pick`, `formatDate`, `statusTitle`, `caseSubjectFromCase`, `applicantLabelFromCase`
- `renderCaseTable`, `renderCommentThreadList`, `renderDocumentList`
- `renderHistoryTimeline` (used by cases-hub history subtab)

Portals and hubs should reuse these instead of duplicating formatters.

## Fund credit

Two related pieces:

1. **`fund-credit-capacity-ui.js`** — read-only widget on guarantee/loan portals at CEO-approval statuses; reads `fundCreditCapacity` from case DTO.
2. **`fund-credit-limits.js`** — CRUD for periodic fund limits on home dashboard; CEO-only.

## Orphan / legacy files (not in index.html)

| File | Status |
|------|--------|
| `js/cases-registry.js` | Implements CEO registry UI; `initCasesRegistry` never called from `app.js` |
| `js/dashboard-analytics.js` | Analytics helpers; not script-tagged |
| `js/guarantee-ceo-credit.js` | Replaced by `fund-credit-limits.js` |
| `workflow-runner.js` | CLI-style full workflow automation; requires DOM elements not present in current `index.html` |

Do not document these as active features unless wired into `index.html` and `app.js` init.

## State model

No global store. State lives in:

- **Module closures** (`state` object per IIFE) — case data, loading flags
- **localStorage** — sessions, config, selected case ids (`app.js`)
- **DOM** — form values, active tab classes

Cross-module coordination uses `CustomEvent` on `document` (see README).

## RTL / locale

`index.html`: `lang="fa" dir="rtl"`. Dates and money use `toLocaleString("fa-IR")` in UI helpers.
