# پنل پرونده‌های مالی (Vanilla JS)

**Non-production** frontend for exercising the Maskan financial platform end-to-end through the API gateway. RTL Persian UI covering investment, guarantee, and loan cases, kanban inbox, role dashboards, and admin screens.

## Stack

| Layer | Choice |
|-------|--------|
| Framework | Vanilla JS (IIFE modules, no bundler) |
| Entry | `index.html` → script tags in fixed order |
| Styling | `styles.css` (custom, no component library) |
| Charts | [Chart.js 4.4.1](https://cdn.jsdelivr.net/npm/chart.js@4.4.1) (dashboard tabs only) |
| API | `fetch` via `window.TestPanel.apiRequest` |
| State | `localStorage` (sessions, config, active case) |
| API base | `config.js` → `TESTPANEL_CONFIG.baseUrl` (default `http://localhost:5081`) |

## Run

Serve over HTTP (not `file://`):

```powershell
cd D:\work\Maskan\Panel\Financial-Core\Frontend
python -m http.server 5500
```

Open `http://localhost:5500/index.html`.

Top bar → **تنظیمات** to change base URL, cases API version (`casesVersion`, default `1`), and dev OTP.

## Project structure

```
Frontend/
  index.html          # Layout, tabs, script load order
  app.js              # Core: auth, apiRequest, TestPanel facade, tab wiring
  config.js           # TESTPANEL_CONFIG + localStorage persistence
  styles.css
  workflow-runner.js  # Headless E2E runner (not loaded by index.html)
  js/
    workflow-model.js           # Investment workflow steps, roles, units
    guarantee-workflow-model.js # Guarantee workflow
    loan-workflow-model.js      # Loan workflow
    ui-components.js            # Shared DOM helpers, tables, comments
    cases-hub.js                # Cases list + detail shell (3 modules)
    portal.js                   # Investment case workflow UI
    guarantee-portal.js         # Guarantee case workflow UI
    loan-portal.js              # Loan case workflow UI
    kanban.js                   # Inbox (action-required / watch)
    fund-credit-capacity-ui.js  # Per-case fund capacity widget
    fund-credit-limits.js       # CEO periodic credit limits (tabDashboard)
    home-dashboard.js           # Role-based home cockpit
    employee-kpi-dashboard.js   # Employee KPI sub-tab
    dashboard-ui.js             # Shared dashboard render helpers
    admin-dashboard.js          # Admin multi-role overview + charts
    admin-users.js              # User/session management
    admin-companies.js          # Company CRUD
    cases-registry.js           # CEO/Admin registry (not wired in index.html)
    dashboard-analytics.js      # Standalone analytics helper (not wired)
    guarantee-ceo-credit.js     # Legacy CEO credit UI (superseded by fund-credit-limits)
```

## Navigation (tabs)

Sidebar `data-tab` values in `index.html`:

| Tab ID | Label | Module |
|--------|-------|--------|
| `tabCases` | پرونده‌ها | `cases-hub.js` + per-module portals |
| `tabInbox` | کارتابل | `kanban.js` |
| `tabAccount` | حساب کاربری | OTP login, saved sessions (`app.js`) |
| `tabDashboard` | صفحه اصلی | `home-dashboard.js`, `employee-kpi-dashboard.js`, `fund-credit-limits.js` |
| `tabAdminDashboard` | داشبورد مدیریت | `admin-dashboard.js` |
| `tabAdminUsers` | کاربران | `admin-users.js` |
| `tabAdminCompanies` | شرکت‌ها | `admin-companies.js` |

Role-gated nav buttons start with class `hidden`; modules call `updateAccessUi` / `updateDashboardNav` on `testpanel:session-changed`.

Cases tab uses nested module tabs (`data-module`: `investment` | `guarantee` | `loan`) and detail subtabs (`workflow` | `attachments` | `history`).

## Global API surface

`app.js` exposes `window.TestPanel` after `DOMContentLoaded`. Feature modules receive it via `init*(panel)`:

```javascript
window.TestPanel = {
  apiRequest, unwrapEnvelope, makeUrl,
  casesBasePath, guaranteeCasesBasePath, guaranteeRenewalsBasePath,
  loanCasesBasePath, kanbanBasePath,
  getActiveSession, saveSessionFromLogin, setActiveSessionId,
  setCurrentCaseId, setGuaranteeCaseId, setLoanCaseId,
  getInvestmentCaseId, getGuaranteeCaseId, getLoanCaseId,
  getCaseModule, setCaseModule, clearInvestmentCaseId,
};
```

Workflow metadata: `WorkflowModel`, `GuaranteeWorkflowModel`, `LoanWorkflowModel`. Shared UI: `UIComponents`, `DashboardUi`, `FundCreditCapacityUi`.

## API path conventions

| Domain | Path pattern | Version source |
|--------|--------------|----------------|
| Identity | `/api/v1/identity/...` | Fixed `v1` |
| Dashboard | `/api/v1/dashboard/...` | Fixed `v1` |
| Analytics | `/api/v1/analytics/...` | Fixed `v1` |
| Cases (investment/guarantee/loan/renewal/kanban/fund-credit) | `/api/v{casesVersion}/...` | `TESTPANEL_CONFIG.casesVersion` |

Per-domain endpoint catalogs (backend-oriented):

- [Investment cases](../docs/frontend/INVESTMENT_CASE_API_GUIDE.md)
- [Guarantee cases](../docs/frontend/GUARANTEE_CASE_API_GUIDE.md)
- [Guarantee renewals](../docs/frontend/GUARANTEE_RENEWAL_API_GUIDE.md)
- [Loan cases](../docs/frontend/LOAN_CASE_API_GUIDE.md)

Frontend integration patterns: [docs/frontend/API_INTEGRATION.md](../docs/frontend/API_INTEGRATION.md).

Architecture and extension guides:

- [docs/frontend/ARCHITECTURE.md](../docs/frontend/ARCHITECTURE.md)
- [docs/frontend/DEVELOPER_WORKFLOWS.md](../docs/frontend/DEVELOPER_WORKFLOWS.md)

## Auth & role switching

OTP flow on **حساب کاربری**: `send-otp` → `verify-otp`. Each login is stored as a **saved session** in `localStorage`; **Use** swaps the active Bearer token.

Identity tokens are **JWE** — roles come from login/profile responses, not client-side JWT decode.

`config.js` includes `workflowPersonas` (test phones/roles) for `workflow-runner.js` automation.

## localStorage keys

| Key | Purpose |
|-----|---------|
| `workflow_test_panel.config.v2` | Base URL, cases version, dev OTP |
| `workflow_test_panel.sessions.v1` | Saved OTP sessions |
| `workflow_test_panel.active_session_id.v1` | Active session id |
| `workflow_test_panel.state.v1` | `caseModule`, `investmentCaseId`, `guaranteeCaseId`, `loanCaseId` |

## Custom events

| Event | When | Typical listeners |
|-------|------|-------------------|
| `testpanel:session-changed` | Login, session switch | Kanban, dashboards, admin tabs, portals |
| `testpanel:case-changed` | Case id/module change | `portal.js`, kanban |
| `testpanel:open-comment-step` | UIComponents comment deep-link | `portal.js` |

## Notes

- Most workflow actions require the correct role; use saved sessions to switch personas.
- `app.js` still contains handlers for legacy debug UI elements removed from `index.html`; active UX lives in `js/*` modules.
- Presigned S3 uploads may fail in-browser if storage CORS is misconfigured; presign/confirm can still be validated separately.
