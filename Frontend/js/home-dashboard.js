(function () {
  const ui = () => window.DashboardUi || {};
  const pick = (...args) => (ui().pick ? ui().pick(...args) : undefined);
  const formatMoney = (n) => (ui().formatMoney ? ui().formatMoney(n) : String(n));
  const formatNum = (n) => (ui().formatNum ? ui().formatNum(n) : String(n));
  const formatDate = (iso) => (ui().formatDate ? ui().formatDate(iso) : String(iso || ""));

  const state = {
    panel: null,
    charts: [],
    selectedRole: "me",
    selectedDepartment: "Financial",
    dashboardData: null,
    loading: false,
    isAdmin: false,
  };

  const CHART_COLORS = ui().CHART_COLORS || ["#6366f1", "#22c55e", "#f59e0b", "#ef4444", "#06b6d4"];

  const ROLE_VIEWS = [
    { id: "admin", label: "Admin", endpoint: "/api/v1/dashboard/admin-overview", roles: ["Admin"] },
    { id: "ceo", label: "CEO", endpoint: "/api/v1/dashboard/ceo", roles: ["Admin", "CEO"] },
    { id: "board", label: "Board", endpoint: "/api/v1/dashboard/board", roles: ["Admin", "CEO", "BoardMember", "InvestmentManager"] },
    { id: "executive", label: "Executive", endpoint: "/api/v1/dashboard/executive", roles: ["Admin", "CEO", "InvestmentManager", "FinancialManager", "TechnicalExpert", "TechnicalManager", "BoardMember"] },
    { id: "department", label: "Department", endpoint: "/api/v1/dashboard/department", roles: ["Admin", "InvestmentExpert", "InvestmentManager", "LegalExpert", "LegalManager", "FinancialExpert", "FinancialManager", "CreditExpert", "CreditManager", "TechnicalExpert", "TechnicalManager"] },
    { id: "applicant", label: "Applicant", endpoint: "/api/v1/dashboard/applicant", roles: ["Admin", "Applicant"] },
  ];

  const ROLE_DEFAULT_MAP = {
    Admin: "admin",
    CEO: "ceo",
    BoardMember: "board",
    InvestmentManager: "executive",
    FinancialManager: "executive",
    TechnicalExpert: "executive",
    TechnicalManager: "executive",
    InvestmentExpert: "department",
    LegalExpert: "department",
    LegalManager: "department",
    FinancialExpert: "department",
    CreditExpert: "department",
    CreditManager: "department",
    Applicant: "applicant",
  };

  const DEPARTMENT_BY_ROLE = {
    InvestmentExpert: "Investment",
    InvestmentManager: "Investment",
    LegalExpert: "Legal",
    LegalManager: "Legal",
    FinancialExpert: "Financial",
    FinancialManager: "Financial",
    CreditExpert: "Credit",
    CreditManager: "Credit",
    TechnicalExpert: "Technical",
    TechnicalManager: "Technical",
  };

  function qs(sel) {
    return document.querySelector(sel);
  }

  function unwrap(body) {
    return body && (body.data != null ? body.data : body.Data != null ? body.Data : body);
  }

  function resolveRole() {
    const s = state.panel?.getActiveSession();
    if (window.WorkflowModel && typeof WorkflowModel.normalizeRole === "function") {
      return WorkflowModel.normalizeRole(s?.userRoleText, s?.userRoleNumber);
    }
    return String(s?.userRoleText || "");
  }

  function canAccessView(viewId, sessionRole) {
    const view = ROLE_VIEWS.find((r) => r.id === viewId);
    if (!view) return false;
    if (sessionRole === "Admin") return true;
    return view.roles.includes(sessionRole);
  }

  function accessibleViews(sessionRole) {
    if (sessionRole === "Admin") return ROLE_VIEWS;
    return ROLE_VIEWS.filter((v) => v.roles.includes(sessionRole));
  }

  const DashboardApi = {
    get(roleId, departmentKey) {
      if (roleId === "me") {
        return state.panel.apiRequest({ method: "GET", path: "/api/v1/dashboard/me" });
      }
      const view = ROLE_VIEWS.find((r) => r.id === roleId);
      if (!view) return Promise.reject(new Error("نقش نامعتبر است."));
      let path = view.endpoint;
      if (roleId === "department" && departmentKey) {
        path += "?departmentKey=" + encodeURIComponent(departmentKey);
      }
      return state.panel.apiRequest({ method: "GET", path });
    },
    refresh() {
      return state.panel.apiRequest({ method: "POST", path: "/api/v1/dashboard/refresh" });
    },
  };

  function destroyCharts() {
    state.charts.forEach((c) => {
      try {
        c.destroy();
      } catch (_) {}
    });
    state.charts = [];
  }

  function chartDefaults() {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          position: "bottom",
          labels: { color: "#cbd5e1", padding: 12, usePointStyle: true, pointStyle: "circle" },
        },
        tooltip: {
          backgroundColor: "rgba(15,23,42,.92)",
          titleColor: "#f1f5f9",
          bodyColor: "#cbd5e1",
          borderColor: "rgba(99,102,241,.4)",
          borderWidth: 1,
          padding: 12,
          cornerRadius: 8,
        },
      },
    };
  }

  function createChart(canvas, config) {
    if (!canvas || typeof Chart === "undefined") return null;
    const chart = new Chart(canvas, config);
    state.charts.push(chart);
    return chart;
  }

  function barConfig(labels, data, label, horizontal) {
    return {
      type: "bar",
      data: {
        labels,
        datasets: [{
          label: label || "",
          data,
          backgroundColor: CHART_COLORS.slice(0, labels.length).map((c) => c + "cc"),
          borderRadius: 6,
          borderSkipped: false,
        }],
      },
      options: {
        ...chartDefaults(),
        indexAxis: horizontal ? "y" : "x",
        plugins: { ...chartDefaults().plugins, legend: { display: !!label, labels: { color: "#cbd5e1" } } },
        scales: {
          x: { ticks: { color: "#94a3b8", font: { size: 11 } }, grid: { color: "rgba(255,255,255,0.05)" } },
          y: { ticks: { color: "#94a3b8", font: { size: 11 } }, grid: { color: "rgba(255,255,255,0.05)" } },
        },
      },
    };
  }

  function extractModules(roleId, raw) {
    if (!raw) return [];
    if (roleId === "admin") return pick(raw, "modules", "Modules") || pick(pick(raw, "executive", "Executive"), "modules", "Modules") || [];
    if (roleId === "ceo" || roleId === "board") return pick(raw, "modules", "Modules") || [];
    if (roleId === "executive") {
      const ex = pick(raw, "executive", "Executive") || raw;
      return pick(ex, "modules", "Modules") || [];
    }
    if (roleId === "department") {
      const dept = pick(raw, "department", "Department") || raw;
      return pick(dept, "modules", "Modules") || [];
    }
    if (roleId === "me") {
      const ex = pick(raw, "executive", "Executive");
      if (ex) return pick(ex, "modules", "Modules") || [];
      const dept = pick(raw, "department", "Department");
      if (dept) return pick(dept, "modules", "Modules") || [];
    }
    return [];
  }

  function extractSystemHealth(roleId, raw) {
    if (!raw) return null;
    if (roleId === "admin") return pick(raw, "systemHealth", "SystemHealth") || pick(pick(raw, "executive", "Executive"), "systemHealth", "SystemHealth");
    if (roleId === "executive" || roleId === "me") {
      const ex = pick(raw, "executive", "Executive") || raw;
      return pick(ex, "systemHealth", "SystemHealth") || pick(raw, "systemHealth", "SystemHealth");
    }
    return null;
  }

  function mapDashboardView(roleId, raw) {
    if (!raw) return { roleId, meta: {}, metrics: [], modules: [], charts: [], panels: [] };

    const modules = extractModules(roleId, raw);
    const systemHealth = extractSystemHealth(roleId, raw);

    if (roleId === "admin") {
      const ex = pick(raw, "executive", "Executive") || {};
      const ceo = pick(raw, "ceo", "Ceo") || {};
      const board = pick(raw, "board", "Board") || {};
      const app = pick(raw, "applicantSummary", "ApplicantSummary") || {};
      return {
        roleId,
        meta: { computedAt: pick(raw, "computedAtUtc", "ComputedAtUtc"), isStale: pick(raw, "isStale", "IsStale") },
        systemHealth,
        metrics: [
          { label: "کل پرونده‌ها", value: formatNum(pick(board, "totalCases", "TotalCases")), accent: true },
          { label: "پرونده فعال", value: formatNum(pick(ex, "totalActiveCases", "TotalActiveCases")), accent: true },
          { label: "نرخ تکمیل", value: (pick(board, "completionRate", "CompletionRate") || 0) + "%" },
          { label: "ریسک کل (حجم فعال)", value: formatMoney(pick(ceo, "totalRiskExposure", "TotalRiskExposure")), accent: true },
          { label: "در انتظار CEO", value: formatNum(pick(ceo, "pendingCeoApprovals", "PendingCeoApprovals")) },
          { label: "متقاضیان", value: formatNum(pick(app, "applicantCount", "ApplicantCount")) },
        ],
        modules: modules.length ? modules : pick(ex, "modules", "Modules") || [],
        charts: [],
        panels: [
          { type: "bottlenecks", data: pick(ex, "departmentBottlenecks", "DepartmentBottlenecks") || pick(ceo, "departmentBottlenecks", "DepartmentBottlenecks") || [] },
          { type: "departments", data: pick(raw, "departments", "Departments") || [] },
          { type: "fundCredit", data: pick(raw, "fundCreditLimits", "FundCreditLimits") },
        ],
        raw,
      };
    }

    if (roleId === "ceo") {
      return {
        roleId,
        meta: {},
        systemHealth: null,
        metrics: [
          { label: "ریسک کل", value: formatMoney(pick(raw, "totalRiskExposure", "TotalRiskExposure")), accent: true },
          { label: "پرونده فعال", value: formatNum(pick(raw, "totalActiveCases", "TotalActiveCases")), accent: true },
          { label: "در انتظار تأیید مدیرعامل", value: formatNum(pick(raw, "pendingCeoApprovals", "PendingCeoApprovals")), accent: true },
          { label: "مبلغ درخواستی", value: formatMoney(pick(raw, "totalRequestedAmount", "TotalRequestedAmount")) },
          { label: "پرداخت تأییدشده", value: formatMoney(pick(raw, "approvedPaymentsSum", "ApprovedPaymentsSum")) },
          { label: "نرخ تکمیل", value: (pick(raw, "completionRate", "CompletionRate") || 0) + "%" },
          { label: "ردشده", value: formatNum(pick(raw, "rejectedCount", "RejectedCount")) },
        ],
        modules,
        charts: [],
        panels: [
          { type: "bottlenecks", data: pick(raw, "departmentBottlenecks", "DepartmentBottlenecks") || [] },
          { type: "activity", data: pick(raw, "recentActivity", "RecentActivity") || [] },
        ],
        raw,
      };
    }

    if (roleId === "board") {
      return {
        roleId,
        meta: {},
        metrics: [
          { label: "کل پرونده‌ها", value: formatNum(pick(raw, "totalCases", "TotalCases")), accent: true },
          { label: "حجم فعال کل", value: formatMoney(pick(raw, "totalActiveVolume", "TotalActiveVolume")), accent: true },
          { label: "نرخ تکمیل", value: (pick(raw, "completionRate", "CompletionRate") || 0) + "%" },
        ],
        modules,
        charts: [],
        panels: [],
        raw,
      };
    }

    if (roleId === "executive" || roleId === "me") {
      const ex = pick(raw, "executive", "Executive") || raw;
      const viewType = pick(raw, "viewType", "ViewType");
      const isDept = viewType === "Department";
      const dept = isDept ? pick(raw, "department", "Department") : null;

      if (isDept && dept) {
        return mapDepartmentView(raw, dept, modules);
      }

      return {
        roleId,
        meta: {
          computedAt: pick(raw, "computedAtUtc", "ComputedAtUtc"),
          isStale: pick(raw, "isStale", "IsStale"),
          viewType,
        },
        systemHealth: pick(ex, "systemHealth", "SystemHealth"),
        metrics: [
          { label: "ضمانت فعال", value: formatMoney(pick(ex, "activeGuaranteesVolume", "ActiveGuaranteesVolume")), accent: true },
          { label: "سرمایه‌گذاری فعال", value: formatMoney(pick(ex, "activeInvestmentsVolume", "ActiveInvestmentsVolume")), accent: true },
          { label: "تسهیلات فعال", value: formatMoney(pick(ex, "activeLoansVolume", "ActiveLoansVolume")), accent: true },
          { label: "پرونده فعال", value: formatNum(pick(ex, "totalActiveCases", "TotalActiveCases")) },
          { label: "نرخ تکمیل", value: (pick(ex, "completionRate", "CompletionRate") || 0) + "%" },
          { label: "کاربران آنلاین", value: formatNum(pick(ex, "onlineUsersCount", "OnlineUsersCount")) },
          { label: "در انتظار مدیرعامل", value: formatNum(pick(ex, "pendingCeoApprovals", "PendingCeoApprovals")) },
        ],
        modules: modules.length ? modules : pick(ex, "modules", "Modules") || [],
        charts: [
          {
            id: "execMonthly",
            title: "خروجی مالی ماهانه (کل)",
            type: "bar",
            labels: (pick(ex, "monthlyFinancialOutput", "MonthlyFinancialOutput") || []).map((m) => (pick(m, "year", "Year") || "") + "/" + (pick(m, "month", "Month") || "")),
            data: (pick(ex, "monthlyFinancialOutput", "MonthlyFinancialOutput") || []).map((m) => pick(m, "amount", "Amount") || 0),
            datasetLabel: "خروجی مالی (ریال)",
          },
        ],
        panels: [
          { type: "bottlenecks", data: pick(ex, "departmentBottlenecks", "DepartmentBottlenecks") || [] },
          { type: "activity", data: pick(ex, "recentActivity", "RecentActivity") || [] },
          { type: "fundCredit", data: pick(raw, "fundCreditLimits", "FundCreditLimits") },
        ],
        raw,
      };
    }

    if (roleId === "department") {
      const dept = pick(raw, "department", "Department") || raw;
      return mapDepartmentView(raw, dept, modules);
    }

    if (roleId === "applicant") {
      const app = pick(raw, "applicant", "Applicant") || raw;
      return {
        roleId,
        meta: {
          computedAt: pick(raw, "computedAtUtc", "ComputedAtUtc"),
          isStale: pick(raw, "isStale", "IsStale"),
        },
        metrics: [
          { label: "اقدام لازم", value: formatNum(pick(app, "pendingActionsCount", "PendingActionsCount")), accent: true },
          { label: "بدهی باقی‌مانده", value: formatMoney(pick(app, "totalRemainingDebt", "TotalRemainingDebt")), accent: true },
          { label: "اقساط پرداخت‌نشده", value: formatNum(pick(app, "unpaidInstallmentsCount", "UnpaidInstallmentsCount")) },
          { label: "پرونده‌های فعال", value: formatNum((pick(app, "activeCases", "ActiveCases") || []).length) },
        ],
        modules: [],
        charts: [],
        panels: [
          { type: "applicantCases", data: pick(app, "activeCases", "ActiveCases") || [] },
          { type: "applicantComments", data: pick(app, "recentComments", "RecentComments") || [] },
        ],
        raw,
      };
    }

    return { roleId, meta: {}, metrics: [], modules: [], charts: [], panels: [], raw };
  }

  function mapDepartmentView(raw, dept, modules) {
    return {
      roleId: "department",
      meta: {
        computedAt: pick(raw, "computedAtUtc", "ComputedAtUtc"),
        isStale: pick(raw, "isStale", "IsStale"),
      },
      metrics: [
        { label: "واحد", value: pick(dept, "departmentTitle", "DepartmentTitle") || "—", accent: true },
        { label: "صف انتظار", value: formatNum(pick(dept, "totalQueueCount", "TotalQueueCount")) },
        { label: "نرخ بازگشت", value: (pick(dept, "revisionRatePercent", "RevisionRatePercent") || 0) + "%" },
      ],
      modules,
      departmentKey: pick(dept, "departmentKey", "DepartmentKey"),
      specificMetrics: pick(dept, "specificMetrics", "SpecificMetrics"),
      charts: [
        {
          id: "deptQueue",
          title: "صف انتظار به تفکیک ماژول",
          type: "bar",
          horizontal: true,
          labels: (pick(dept, "queueByModule", "QueueByModule") || []).map((q) => pick(q, "moduleTitle", "ModuleTitle") || ""),
          data: (pick(dept, "queueByModule", "QueueByModule") || []).map((q) => pick(q, "count", "Count") || 0),
          datasetLabel: "تعداد پرونده",
        },
      ],
      panels: [{ type: "inbox", data: pick(dept, "inboxQuickLinks", "InboxQuickLinks") || [] }],
      raw,
    };
  }

  function setLoading(on) {
    state.loading = on;
    qs("#homeDashboardLoading")?.classList.toggle("hidden", !on);
    qs("#homeDashboardContent")?.classList.toggle("home-dashboard__content--loading", on);
    qs("#homeRoleSwitcher")?.toggleAttribute("disabled", on);
    qs("#homeDepartmentSwitcher")?.toggleAttribute("disabled", on);
    qs("#btnHomeLoad")?.toggleAttribute("disabled", on);
    qs("#btnHomeRefreshCache")?.toggleAttribute("disabled", on);
  }

  function renderMetrics(metrics) {
    const host = qs("#homeKpiStrip");
    if (!host) return;
    host.innerHTML = (metrics || [])
      .map(
        (m) =>
          '<div class="home-kpi' +
          (m.accent ? " home-kpi--accent" : "") +
          '"><span class="home-kpi__label">' +
          m.label +
          '</span><strong class="home-kpi__value">' +
          m.value +
          "</strong></div>"
      )
      .join("");
  }

  function renderCharts(charts) {
    const host = qs("#homeChartsGrid");
    if (!host) return;
    host.innerHTML = "";
    (charts || []).forEach((ch, idx) => {
      if (!ch.labels?.length && !ch.data?.length) return;
      const card = document.createElement("div");
      card.className = "home-chart-card";
      card.innerHTML =
        '<div class="home-chart-card__title">' +
        (ch.title || "") +
        '</div><div class="home-chart-card__canvas"><canvas id="homeChart_' +
        idx +
        '"></canvas></div>';
      host.appendChild(card);
      const canvas = qs("#homeChart_" + idx);
      createChart(canvas, barConfig(ch.labels, ch.data, ch.datasetLabel, ch.horizontal));
    });
    if (!host.children.length) host.classList.add("hidden");
    else host.classList.remove("hidden");
  }

  function renderPanels(panels) {
    const host = qs("#homePanelsArea");
    if (!host) return;
    host.innerHTML = "";

    (panels || []).forEach((panel) => {
      if (panel.type === "deptMetrics" && panel.data) {
        const el = document.createElement("div");
        el.className = "home-panel card";
        el.innerHTML =
          '<div class="card__title">شاخص‌های تخصصی واحد</div><div class="home-panel__body">' +
          (ui().renderDepartmentMetrics ? ui().renderDepartmentMetrics(panel.data, panel.departmentKey) : "") +
          "</div>";
        host.appendChild(el);
      }

      if (panel.type === "bottlenecks") {
        const el = document.createElement("div");
        el.className = "home-panel card";
        el.innerHTML = '<div class="card__title">گلوگاه‌های واحدها</div><div class="home-panel__body"></div>';
        const body = el.querySelector(".home-panel__body");
        if (!panel.data?.length) body.innerHTML = '<p class="muted">داده‌ای موجود نیست.</p>';
        else {
          body.innerHTML = panel.data
            .map((b) => {
              const title = pick(b, "departmentTitle", "DepartmentTitle") || "";
              const days = pick(b, "averageDays", "AverageDays") || 0;
              const cnt = pick(b, "activeCaseCount", "ActiveCaseCount") || 0;
              const pct = Math.min(100, Math.round(days * 3));
              return (
                '<div class="dashboard-bar"><div class="dashboard-bar__label"><span>' +
                title +
                ' <span class="muted">(' +
                cnt +
                ' پرونده)</span></span><span class="mono">' +
                days +
                " روز</span></div><div class=\"dashboard-bar__track\"><div class=\"dashboard-bar__fill dashboard-bar__fill--warn\" style=\"width:" +
                pct +
                '%"></div></div></div>'
              );
            })
            .join("");
        }
        host.appendChild(el);
      }

      if (panel.type === "activity") {
        const el = document.createElement("div");
        el.className = "home-panel card";
        const rows = panel.data || [];
        el.innerHTML =
          '<div class="card__title">فعالیت اخیر</div><ul class="dashboard-activity">' +
          (rows.length
            ? rows
                .map((r) => {
                  const cn = pick(r, "caseNumber", "CaseNumber") || "";
                  const act = pick(r, "action", "Action") || "";
                  const at = pick(r, "createdAt", "CreatedAt") || "";
                  return '<li><span class="mono">' + cn + "</span> — " + act + ' <span class="muted">' + formatDate(at) + "</span></li>";
                })
                .join("")
            : '<li class="muted">فعالیتی ثبت نشده است.</li>') +
          "</ul>";
        host.appendChild(el);
      }

      if (panel.type === "inbox") {
        const el = document.createElement("div");
        el.className = "home-panel card";
        const links = panel.data || [];
        el.innerHTML = '<div class="card__title">کارتابل — اقدام فوری</div>';
        if (!links.length) el.innerHTML += '<p class="muted">موردی در صف اقدام شما نیست.</p>';
        else {
          const ul = document.createElement("ul");
          ul.className = "dashboard-inbox";
          links.forEach((l) => {
            const mod = (pick(l, "module", "Module") || "").toLowerCase();
            const cn = pick(l, "caseNumber", "CaseNumber") || "";
            const st = pick(l, "statusTitle", "StatusTitle") || "";
            const id = pick(l, "caseId", "CaseId") || "";
            const li = document.createElement("li");
            const btn = document.createElement("button");
            btn.type = "button";
            btn.className = "btn btn--small dash-inbox-link";
            btn.innerHTML = '<span class="mono">' + cn + "</span> — " + st;
            btn.addEventListener("click", () => {
              if (window.CasesHub && id) {
                document.querySelector('[data-tab="tabCases"]')?.click();
                window.CasesHub.openCase(mod, id);
              }
            });
            li.appendChild(btn);
            ul.appendChild(li);
          });
          el.appendChild(ul);
        }
        host.appendChild(el);
      }

      if (panel.type === "applicantCases") {
        const el = document.createElement("div");
        el.className = "home-panel card";
        const cases = panel.data || [];
        let html = '<div class="card__title">پرونده‌های فعال</div>';
        if (!cases.length) html += '<p class="muted">پرونده فعالی ندارید.</p>';
        else {
          html += cases
            .map((c) => {
              const pct = pick(c, "progressPercent", "ProgressPercent") || 0;
              const cn = pick(c, "caseNumber", "CaseNumber") || "";
              const mod = pick(c, "moduleTitle", "ModuleTitle") || "";
              const st = pick(c, "statusTitle", "StatusTitle") || "";
              const ph = pick(c, "phaseTitle", "PhaseTitle") || "";
              const id = pick(c, "caseId", "CaseId") || "";
              const moduleKey = (pick(c, "module", "Module") || "").toLowerCase();
              return (
                '<div class="dash-progress-card"><div class="dash-progress-card__head"><button type="button" class="btn btn--small home-case-link" data-module="' +
                moduleKey +
                '" data-case-id="' +
                id +
                '"><span class="mono">' +
                cn +
                '</span></button><span class="muted">' +
                mod +
                "</span></div><div class=\"muted\">" +
                ph +
                " — " +
                st +
                '</div><div class="dash-stepper"><div class="dash-stepper__fill" style="width:' +
                pct +
                '%"></div></div><div class="muted dash-stepper__label">' +
                pct +
                "% پیشرفت</div></div>"
              );
            })
            .join("");
        }
        el.innerHTML = html;
        el.querySelectorAll(".home-case-link").forEach((btn) => {
          btn.addEventListener("click", () => {
            const mod = btn.getAttribute("data-module");
            const caseId = btn.getAttribute("data-case-id");
            if (window.CasesHub && caseId) {
              document.querySelector('[data-tab="tabCases"]')?.click();
              window.CasesHub.openCase(mod, caseId);
            }
          });
        });
        host.appendChild(el);
      }

      if (panel.type === "applicantComments") {
        const el = document.createElement("div");
        el.className = "home-panel card";
        const comments = panel.data || [];
        el.innerHTML =
          '<div class="card__title">آخرین نظرات</div><ul class="dashboard-activity">' +
          (comments.length
            ? comments
                .map((c) => {
                  const cn = pick(c, "caseNumber", "CaseNumber") || "";
                  const msg = pick(c, "commentText", "CommentText") || "";
                  const role = pick(c, "authorRole", "AuthorRole") || "";
                  const at = pick(c, "createdAt", "CreatedAt") || "";
                  return '<li><span class="mono">' + cn + '</span> <span class="muted">(' + role + ")</span>: " + msg + ' <span class="muted">' + formatDate(at) + "</span></li>";
                })
                .join("")
            : '<li class="muted">نظری ثبت نشده است.</li>') +
          "</ul>";
        host.appendChild(el);
      }

      if (panel.type === "departments") {
        const el = document.createElement("div");
        el.className = "home-panel card";
        const departments = panel.data || [];
        if (!departments.length) {
          el.innerHTML = '<div class="card__title">واحدهای سازمانی</div><p class="muted">داده واحدها موجود نیست.</p>';
        } else {
          el.innerHTML =
            '<div class="card__title">واحدهای سازمانی</div><div class="home-dept-grid">' +
            departments
              .map((dept) => {
                const title = pick(dept, "departmentTitle", "DepartmentTitle") || "";
                const queue = pick(dept, "totalQueueCount", "TotalQueueCount") || 0;
                const revision = pick(dept, "revisionRatePercent", "RevisionRatePercent") || 0;
                const sm = pick(dept, "specificMetrics", "SpecificMetrics");
                let extra = "";
                if (sm && pick(sm, "pendingFinancialReviews", "PendingFinancialReviews") != null) {
                  extra = " · بررسی مالی " + formatNum(pick(sm, "pendingFinancialReviews", "PendingFinancialReviews"));
                }
                return '<div class="home-dept-card"><strong>' + title + '</strong><span class="muted">' + formatNum(queue) + " در صف · بازگشت " + revision + "%" + extra + "</span></div>";
              })
              .join("") +
            "</div>";
        }
        host.appendChild(el);
      }

      if (panel.type === "fundCredit" && panel.data) {
        const el = document.createElement("div");
        el.className = "home-panel card fund-credit-dash";
        const active = pick(panel.data, "activePools", "ActivePools") || [];
        const historical = pick(panel.data, "historicalPools", "HistoricalPools") || [];
        let html = '<div class="card__title">سقف اعتبار دوره‌ای صندوق</div>';
        const renderPools = (pools, title) => {
          if (!pools.length) return "";
          let block = '<div class="fund-credit-dash__section"><div class="muted">' + title + "</div>";
          pools.forEach((p) => {
            const modLabel = Number(pick(p, "moduleType", "ModuleType")) === 2 ? "تسهیلات" : "ضمانت‌نامه";
            block +=
              '<div class="fund-credit-dash__card"><strong>' +
              modLabel +
              "</strong> · " +
              (pick(p, "periodStart", "PeriodStart") || "—") +
              " تا " +
              (pick(p, "expiresAt", "ExpiresAt") || "—") +
              "<br/>بودجه: " +
              formatMoney(pick(p, "creditLimitWithCheck", "CreditLimitWithCheck")) +
              " · مصرف: " +
              formatMoney(pick(p, "totalUtilized", "TotalUtilized")) +
              " · مانده: " +
              formatMoney(pick(p, "remainingCapacity", "RemainingCapacity")) +
              "</div>";
          });
          return block + "</div>";
        };
        html += renderPools(active, "دوره‌های فعال") + renderPools(historical, "تاریخچه");
        el.innerHTML = html;
        host.appendChild(el);
      }
    });
  }

  function renderViewModel(vm) {
    const healthHost = qs("#homeSystemHealth");
    if (healthHost) {
      healthHost.innerHTML = vm.systemHealth && ui().renderSystemHealth ? ui().renderSystemHealth(vm.systemHealth) : "";
      healthHost.classList.toggle("hidden", !vm.systemHealth);
    }

    renderMetrics(vm.metrics);

    if (ui().renderModuleSections) {
      ui().renderModuleSections(qs("#homeModuleSections"), vm.modules, state.charts, "homeMod");
      qs("#homeModuleSections")?.classList.toggle("hidden", !(vm.modules && vm.modules.length));
    }

    if (vm.specificMetrics) {
      vm.panels = vm.panels || [];
      vm.panels.unshift({ type: "deptMetrics", data: vm.specificMetrics, departmentKey: vm.departmentKey });
    }

    renderCharts(vm.charts);
    renderPanels(vm.panels);

    const metaEl = qs("#homeDashboardMeta");
    const roleLabel =
      vm.roleId === "me"
        ? "شخصی"
        : ROLE_VIEWS.find((r) => r.id === vm.roleId)?.label || vm.roleId;
    if (metaEl) {
      let text = "نمای " + roleLabel;
      if (vm.meta?.viewType) text += " (" + vm.meta.viewType + ")";
      if (vm.meta?.computedAt) text += " · به‌روزرسانی: " + formatDate(vm.meta.computedAt);
      if (vm.meta?.isStale) text += " · (داده قدیمی)";
      metaEl.textContent = text;
    }
  }

  function updateAdminControls() {
    const role = resolveRole();
    state.isAdmin = role === "Admin";
    qs("#homeAdminControls")?.classList.toggle("hidden", !state.isAdmin);
    qs("#btnHomeLoad")?.classList.toggle("hidden", !state.isAdmin);
    qs("#btnHomeRefreshCache")?.classList.toggle("hidden", !state.isAdmin);
    toggleDepartmentSwitcher();
  }

  function toggleDepartmentSwitcher() {
    const roleId = qs("#homeRoleSwitcher")?.value || state.selectedRole;
    const show = state.isAdmin && roleId === "department";
    qs(".home-dept-switcher")?.classList.toggle("hidden", !show);
  }

  function populateRoleSwitcher() {
    const select = qs("#homeRoleSwitcher");
    if (!select) return;
    const role = resolveRole();
    const views = accessibleViews(role);
    select.innerHTML = "";
    views.forEach((r) => {
      const opt = document.createElement("option");
      opt.value = r.id;
      opt.textContent = r.label;
      select.appendChild(opt);
    });
  }

  function defaultRoleForSession() {
    const role = resolveRole();
    if (role === "Admin") return "admin";
    return "me";
  }

  async function loadDashboard(roleId, refreshCache) {
    const errEl = qs("#homeDashboardError");
    errEl?.classList.add("hidden");
    destroyCharts();
    setLoading(true);

    try {
      if (refreshCache && state.isAdmin) await DashboardApi.refresh();

      const departmentKey =
        roleId === "department" && state.isAdmin ? qs("#homeDepartmentSwitcher")?.value || state.selectedDepartment : null;

      const res = await DashboardApi.get(roleId, departmentKey);
      const raw = unwrap(res.body);
      if (!raw) throw new Error("پاسخ داشبورد خالی است.");

      state.dashboardData = raw;
      state.selectedRole = roleId;
      const vm = mapDashboardView(roleId, raw);
      renderViewModel(vm);
    } catch (e) {
      if (errEl) {
        errEl.textContent = String(e.message || e);
        errEl.classList.remove("hidden");
      }
      qs("#homeKpiStrip").innerHTML = "";
      qs("#homeModuleSections").innerHTML = "";
      qs("#homeChartsGrid").innerHTML = "";
      qs("#homePanelsArea").innerHTML = "";
      qs("#homeSystemHealth").innerHTML = "";
    } finally {
      setLoading(false);
    }
  }

  function updateNav() {
    const loggedIn = !!state.panel?.getActiveSession()?.accessToken;
    qs("#navDashboard")?.classList.toggle("hidden", !loggedIn);
    updateAdminControls();
  }

  function wire() {
    populateRoleSwitcher();
    updateAdminControls();

    const role = resolveRole();
    const deptSelect = qs("#homeDepartmentSwitcher");
    if (deptSelect && DEPARTMENT_BY_ROLE[role]) deptSelect.value = DEPARTMENT_BY_ROLE[role];

    qs("#homeRoleSwitcher")?.addEventListener("change", (e) => {
      toggleDepartmentSwitcher();
      if (state.isAdmin) void loadDashboard(e.target.value, false);
    });

    qs("#homeDepartmentSwitcher")?.addEventListener("change", (e) => {
      state.selectedDepartment = e.target.value;
      if (state.isAdmin && (qs("#homeRoleSwitcher")?.value || "") === "department") {
        void loadDashboard("department", false);
      }
    });

    qs("#btnHomeLoad")?.addEventListener("click", () => {
      const roleId = qs("#homeRoleSwitcher")?.value || state.selectedRole;
      void loadDashboard(roleId, false);
    });

    qs("#btnHomeRefreshCache")?.addEventListener("click", () => {
      const roleId = state.isAdmin ? qs("#homeRoleSwitcher")?.value || state.selectedRole : "me";
      void loadDashboard(roleId, true);
    });

    document.addEventListener("testpanel:session-changed", () => {
      updateNav();
      populateRoleSwitcher();
      const defaultRole = defaultRoleForSession();
      const select = qs("#homeRoleSwitcher");
      if (select && state.isAdmin) select.value = defaultRole === "me" ? "admin" : defaultRole;
      state.selectedRole = state.isAdmin ? select?.value || "admin" : "me";
      if (qs("#tabDashboard")?.classList.contains("is-active")) {
        void loadDashboard(state.selectedRole, false);
      }
    });

    document.querySelector('[data-tab="tabDashboard"]')?.addEventListener("click", () => {
      if (!state.dashboardData) void loadDashboard(state.selectedRole, false);
    });
  }

  window.initHomeDashboard = function initHomeDashboard(panel) {
    state.panel = panel;
    wire();
    const role = resolveRole();
    state.isAdmin = role === "Admin";
    state.selectedRole = state.isAdmin ? ROLE_DEFAULT_MAP[role] || "admin" : "me";
    const select = qs("#homeRoleSwitcher");
    if (select && state.isAdmin) select.value = state.selectedRole;
    updateNav();
  };

  window.HomeDashboardApi = DashboardApi;
  window.mapHomeDashboardView = mapDashboardView;
})();
