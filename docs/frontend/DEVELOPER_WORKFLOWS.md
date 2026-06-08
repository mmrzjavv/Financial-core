# Developer workflows

Actionable steps for extending the vanilla frontend. All paths relative to `Frontend/`.

## Add a sidebar tab

1. **HTML** — In `index.html`:
   - Add `<button class="navbtn" data-tab="tabMyFeature">...</button>` in `.sidebar`
   - Add `<div id="tabMyFeature" class="tab">...</div>` in `.content`

2. **Module** — Create `js/my-feature.js`:

```javascript
(function () {
  const state = { panel: null };

  window.initMyFeature = function (panel) {
    state.panel = panel;
    // wire buttons, listen for testpanel:session-changed
  };
})();
```

3. **Register script** — Add `<script src="js/my-feature.js"></script>` before `app.js` in `index.html`.

4. **Init** — In `app.js` `init()`:

```javascript
if (typeof window.initMyFeature === "function") window.initMyFeature(window.TestPanel);
```

5. **Optional role gate** — Toggle nav visibility on `testpanel:session-changed` (copy `cases-hub.js` → `updateDashboardNav` or `admin-users.js` → `updateAccessUi`).

`wireTabs()` in `app.js` already handles tab switching; add a branch if the tab needs refresh on enter (like `tabInbox` → `kanbanRefresh`).

## Add a case module tab (fourth product line)

Follow the investment/guarantee/loan pattern:

1. Add `js/my-workflow-model.js` — steps, statuses, `window.MyWorkflowModel`.
2. Add `js/my-portal.js` — `initMyPortal(panel)`, listen `testpanel:case-changed`.
3. Extend `cases-hub.js`:
   - `MODULES.myModule` with `apiBase`, `setId`, `root`
   - Create panel in `index.html` under `#caseSubWorkflow`
   - Module tab button in `.cases-module-tabs`
4. Add `myCasesBasePath()` to `app.js` and expose on `TestPanel`.
5. Tag script files in `index.html` before `cases-hub.js`.
6. Call `initMyPortal` from `app.js` init.

## Add an API call to an existing portal

Example: new POST action on guarantee case.

1. Open `js/guarantee-portal.js` — find `actionsForStatus` or the stage builder (grep `method: "POST"`).
2. Add action entry: `{ id, label, method: "POST", path: "/my-action", needsMessage?: true }`.
3. Generic handler already posts to `guaranteeCasesBasePath() + "/" + caseId + path` — verify path matches backend.
4. If new fields are needed, add form rows in the stage renderer for that status.
5. After success, call existing `loadCase()` refresh.

For investment cases, same pattern in `js/portal.js`. Loan: `js/loan-portal.js`.

## Add a shared UI helper

1. Add function to `js/ui-components.js`.
2. Export on `window.UIComponents = { ..., myHelper }`.
3. Use from portals/hubs as `UIComponents.myHelper(...)`.

Keep DOM creation consistent: internal `el(tag, cls, text)` helper pattern.

## Add a dashboard widget

1. **Data** — Extend fetch in `home-dashboard.js` or `admin-dashboard.js` (or add query param to existing dashboard endpoint).
2. **Render** — Add function in `dashboard-ui.js` if reusable; otherwise render in the dashboard module.
3. **Charts** — Use Chart.js like existing canvases; store instances in `state.charts` and call `destroyCharts()` before re-render.

## Wire an orphan module

`cases-registry.js` is an example of implemented but unwired code:

1. Add DOM host in `index.html` (or reuse a tab).
2. Script tag in `index.html`.
3. `initCasesRegistry(window.TestPanel)` in `app.js`.
4. Expose nav button + `refreshCasesRegistryAccess` on session change.

## Configure test personas

Edit `config.js` → `workflowPersonas` (phone, role number, label). Used by `workflow-runner.js` for automated multi-role flows. Dev OTP default: `devOtp` (`123456`).

## Manual test checklist

1. Serve `Frontend/` over HTTP.
2. Set API base URL to running gateway (default port 5081).
3. Login via OTP on **حساب کاربری**; save multiple sessions for role switching.
4. Exercise feature tab; verify network calls in browser DevTools.
5. If 403, switch session role; if envelope error, read `message` / `validationErrors`.

## What not to do

- Do not add npm/webpack unless the project explicitly moves to a build pipeline.
- Do not hardcode `localhost` in modules — use `TestPanel` / `TESTPANEL_CONFIG`.
- Do not decode JWT for roles — use session fields from login.
- Do not duplicate workflow status maps — extend the appropriate `*-workflow-model.js`.
