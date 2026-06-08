(function () {
  const state = { panel: null, charts: [] };

  function qs(sel) {
    return document.querySelector(sel);
  }

  function pick(obj, ...keys) {
    if (!obj) return undefined;
    for (const k of keys) {
      if (obj[k] !== undefined && obj[k] !== null) return obj[k];
    }
    return undefined;
  }

  function resolveRole() {
    const s = state.panel?.getActiveSession();
    return WorkflowModel.normalizeRole(s?.userRoleText, s?.userRoleNumber);
  }

  function formatMoney(n) {
    const v = Number(n) || 0;
    return v.toLocaleString("fa-IR") + " ریال";
  }

  function formatDate(iso) {
    if (!iso) return "—";
    try {
      return new Date(iso).toLocaleString("fa-IR");
    } catch {
      return String(iso);
    }
  }

  function unwrap(body) {
    return body && (body.data != null ? body.data : body.Data != null ? body.Data : body);
  }

  function destroyCharts() {
    state.charts.forEach((c) => {
      try {
        c.destroy();
      } catch (_) {}
    });
    state.charts = [];
  }

  function createChart(canvas, config) {
    if (!canvas || typeof Chart === "undefined") return null;
    const chart = new Chart(canvas, config);
    state.charts.push(chart);
    return chart;
  }

  function showSection(viewType) {
    ["Executive", "Department", "Applicant"].forEach((t) => {
      qs("#dashView" + t)?.classList.toggle("hidden", t !== viewType);
    });
  }

  function renderExecutive(ex) {
    const metrics = qs("#dashExecMetrics");
    if (!metrics) return;
    metrics.innerHTML =
      '<div class="dashboard-metric dashboard-metric--accent"><span class="muted">ضمانت فعال</span><strong>' +
      formatMoney(pick(ex, "activeGuaranteesVolume", "ActiveGuaranteesVolume")) +
      "</strong></div>" +
      '<div class="dashboard-metric dashboard-metric--accent"><span class="muted">سرمایه‌گذاری فعال</span><strong>' +
      formatMoney(pick(ex, "activeInvestmentsVolume", "ActiveInvestmentsVolume")) +
      "</strong></div>" +
      '<div class="dashboard-metric dashboard-metric--accent"><span class="muted">تسهیلات فعال</span><strong>' +
      formatMoney(pick(ex, "activeLoansVolume", "ActiveLoansVolume")) +
      "</strong></div>" +
      '<div class="dashboard-metric"><span class="muted">پرونده فعال</span><strong>' +
      (pick(ex, "totalActiveCases", "TotalActiveCases") || 0) +
      "</strong></div>" +
      '<div class="dashboard-metric"><span class="muted">تکمیل‌شده</span><strong>' +
      (pick(ex, "completedCases", "CompletedCases") || 0) +
      "</strong></div>" +
      '<div class="dashboard-metric"><span class="muted">نرخ تکمیل</span><strong>' +
      (pick(ex, "completionRate", "CompletionRate") || 0) +
      "%</strong></div>" +
      '<div class="dashboard-metric"><span class="muted">کاربران آنلاین</span><strong>' +
      (pick(ex, "onlineUsersCount", "OnlineUsersCount") || 0) +
      "</strong></div>" +
      '<div class="dashboard-metric"><span class="muted">کاربران فعال امروز</span><strong>' +
      (pick(ex, "dailyActiveUsers", "DailyActiveUsers") || 0) +
      "</strong></div>" +
      '<div class="dashboard-metric"><span class="muted">در انتظار مدیرعامل</span><strong>' +
      (pick(ex, "pendingCeoApprovals", "PendingCeoApprovals") || 0) +
      "</strong></div>" +
      '<div class="dashboard-metric"><span class="muted">میانگین روز بررسی</span><strong>' +
      (pick(ex, "averageDaysInReview", "AverageDaysInReview") || 0) +
      "</strong></div>";

    const statusDist = pick(ex, "statusDistribution", "StatusDistribution") || [];
    createChart(qs("#dashExecPie"), {
      type: "doughnut",
      data: {
        labels: statusDist.map((x) => pick(x, "categoryTitle", "CategoryTitle") || ""),
        datasets: [
          {
            data: statusDist.map((x) => pick(x, "count", "Count") || 0),
            backgroundColor: ["#3b82f6", "#22c55e", "#ef4444"],
            borderWidth: 0,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { position: "bottom", labels: { color: "#cbd5e1" } } },
      },
    });

    const monthly = pick(ex, "monthlyFinancialOutput", "MonthlyFinancialOutput") || [];
    createChart(qs("#dashExecBar"), {
      type: "bar",
      data: {
        labels: monthly.map((m) => (pick(m, "year", "Year") || "") + "/" + (pick(m, "month", "Month") || "")),
        datasets: [
          {
            label: "خروجی مالی (ریال)",
            data: monthly.map((m) => pick(m, "amount", "Amount") || 0),
            backgroundColor: "rgba(59,130,246,0.7)",
            borderRadius: 6,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: { ticks: { color: "#94a3b8" }, grid: { color: "rgba(255,255,255,0.06)" } },
          y: { ticks: { color: "#94a3b8" }, grid: { color: "rgba(255,255,255,0.06)" } },
        },
        plugins: { legend: { labels: { color: "#cbd5e1" } } },
      },
    });

    const bottlenecks = pick(ex, "departmentBottlenecks", "DepartmentBottlenecks") || [];
    const bnEl = qs("#dashExecBottlenecks");
    if (bnEl) {
      bnEl.innerHTML =
        '<div class="card__title">گلوگاه‌های واحدها (میانگین روز)</div>' +
        (bottlenecks.length
          ? bottlenecks
              .map((b) => {
                const title = pick(b, "departmentTitle", "DepartmentTitle") || "";
                const days = pick(b, "averageDays", "AverageDays") || 0;
                const cnt = pick(b, "activeCaseCount", "ActiveCaseCount") || 0;
                const pct = Math.min(100, Math.round(days * 3));
                return (
                  '<div class="dashboard-bar">' +
                  '<div class="dashboard-bar__label"><span>' +
                  title +
                  ' <span class="muted">(' +
                  cnt +
                  ' پرونده)</span></span><span class="mono">' +
                  days +
                  " روز</span></div>" +
                  '<div class="dashboard-bar__track"><div class="dashboard-bar__fill dashboard-bar__fill--warn" style="width:' +
                  pct +
                  '%"></div></div></div>'
                );
              })
              .join("")
          : '<p class="muted">داده‌ای موجود نیست.</p>');
    }

    const activity = pick(ex, "recentActivity", "RecentActivity") || [];
    const actEl = qs("#dashExecActivity");
    if (actEl) {
      actEl.innerHTML =
        '<div class="card__title">فعالیت اخیر</div><ul class="dashboard-activity">' +
        activity
          .map((r) => {
            const cn = pick(r, "caseNumber", "CaseNumber") || "";
            const act = pick(r, "action", "Action") || "";
            const at = pick(r, "createdAt", "CreatedAt") || "";
            return '<li><span class="mono">' + cn + "</span> — " + act + ' <span class="muted">' + formatDate(at) + "</span></li>";
          })
          .join("") +
        "</ul>";
    }
  }

  function renderDepartment(dept) {
    const metrics = qs("#dashDeptMetrics");
    if (!metrics) return;
    const title = pick(dept, "departmentTitle", "DepartmentTitle") || "";
    metrics.innerHTML =
      '<div class="dashboard-metric dashboard-metric--accent"><span class="muted">واحد</span><strong>' +
      title +
      "</strong></div>" +
      '<div class="dashboard-metric"><span class="muted">صف انتظار</span><strong>' +
      (pick(dept, "totalQueueCount", "TotalQueueCount") || 0) +
      "</strong></div>" +
      '<div class="dashboard-metric"><span class="muted">نرخ بازگشت</span><strong>' +
      (pick(dept, "revisionRatePercent", "RevisionRatePercent") || 0) +
      "%</strong></div>";

    const queue = pick(dept, "queueByModule", "QueueByModule") || [];
    createChart(qs("#dashDeptBar"), {
      type: "bar",
      data: {
        labels: queue.map((q) => pick(q, "moduleTitle", "ModuleTitle") || ""),
        datasets: [
          {
            label: "تعداد پرونده",
            data: queue.map((q) => pick(q, "count", "Count") || 0),
            backgroundColor: ["#8b5cf6", "#06b6d4", "#f59e0b", "#10b981"],
            borderRadius: 6,
          },
        ],
      },
      options: {
        indexAxis: "y",
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: { ticks: { color: "#94a3b8" }, grid: { color: "rgba(255,255,255,0.06)" } },
          y: { ticks: { color: "#94a3b8" }, grid: { display: false } },
        },
        plugins: { legend: { display: false } },
      },
    });

    const links = pick(dept, "inboxQuickLinks", "InboxQuickLinks") || [];
    const inboxEl = qs("#dashDeptInbox");
    if (inboxEl) {
      inboxEl.innerHTML =
        '<div class="card__title">کارتابل — اقدام فوری</div>' +
        (links.length
          ? '<ul class="dashboard-inbox">' +
            links
              .map((l) => {
                const mod = pick(l, "module", "Module") || "";
                const cn = pick(l, "caseNumber", "CaseNumber") || "";
                const st = pick(l, "statusTitle", "StatusTitle") || "";
                const id = pick(l, "caseId", "CaseId") || "";
                return (
                  '<li><button type="button" class="btn btn--small dash-inbox-link" data-module="' +
                  mod.toLowerCase() +
                  '" data-case-id="' +
                  id +
                  '"><span class="mono">' +
                  cn +
                  "</span> — " +
                  st +
                  "</button></li>"
                );
              })
              .join("") +
            "</ul>"
          : '<p class="muted">موردی در صف اقدام شما نیست.</p>');

      inboxEl.querySelectorAll(".dash-inbox-link").forEach((btn) => {
        btn.addEventListener("click", () => {
          const mod = btn.getAttribute("data-module");
          const caseId = btn.getAttribute("data-case-id");
          if (window.CasesHub && caseId) {
            document.querySelector('[data-tab="tabCases"]')?.click();
            window.CasesHub.openCase(mod, caseId);
          }
        });
      });
    }
  }

  function renderApplicant(app) {
    const metrics = qs("#dashApplicantMetrics");
    if (!metrics) return;
    metrics.innerHTML =
      '<div class="dashboard-metric"><span class="muted">بدهی باقی‌مانده</span><strong>' +
      formatMoney(pick(app, "totalRemainingDebt", "TotalRemainingDebt")) +
      "</strong></div>" +
      '<div class="dashboard-metric"><span class="muted">اقساط پرداخت‌نشده</span><strong>' +
      (pick(app, "unpaidInstallmentsCount", "UnpaidInstallmentsCount") || 0) +
      "</strong></div>";

    const cases = pick(app, "activeCases", "ActiveCases") || [];
    const casesEl = qs("#dashApplicantCases");
    if (casesEl) {
      casesEl.innerHTML =
        '<div class="card__title">پرونده‌های فعال</div>' +
        (cases.length
          ? cases
              .map((c) => {
                const pct = pick(c, "progressPercent", "ProgressPercent") || 0;
                const cn = pick(c, "caseNumber", "CaseNumber") || "";
                const mod = pick(c, "moduleTitle", "ModuleTitle") || "";
                const st = pick(c, "statusTitle", "StatusTitle") || "";
                const ph = pick(c, "phaseTitle", "PhaseTitle") || "";
                const id = pick(c, "caseId", "CaseId") || "";
                const moduleKey = (pick(c, "module", "Module") || "").toLowerCase();
                return (
                  '<div class="dash-progress-card">' +
                  '<div class="dash-progress-card__head"><button type="button" class="btn btn--small dash-case-link" data-module="' +
                  moduleKey +
                  '" data-case-id="' +
                  id +
                  '"><span class="mono">' +
                  cn +
                  "</span></button><span class="muted">" +
                  mod +
                  "</span></div>" +
                  '<div class="muted">' +
                  ph +
                  " — " +
                  st +
                  "</div>" +
                  '<div class="dash-stepper"><div class="dash-stepper__fill" style="width:' +
                  pct +
                  '%"></div></div>" +
                  '<div class="muted dash-stepper__label">' +
                  pct +
                  "% پیشرفت</div></div>"
                );
              })
              .join("")
          : '<p class="muted">پرونده فعالی ندارید.</p>');

      casesEl.querySelectorAll(".dash-case-link").forEach((btn) => {
        btn.addEventListener("click", () => {
          const mod = btn.getAttribute("data-module");
          const caseId = btn.getAttribute("data-case-id");
          if (window.CasesHub && caseId) {
            document.querySelector('[data-tab="tabCases"]')?.click();
            window.CasesHub.openCase(mod, caseId);
          }
        });
      });
    }

    const comments = pick(app, "recentComments", "RecentComments") || [];
    const commEl = qs("#dashApplicantComments");
    if (commEl) {
      commEl.innerHTML =
        '<div class="card__title">آخرین نظرات</div><ul class="dashboard-activity">' +
        (comments.length
          ? comments
              .map((c) => {
                const cn = pick(c, "caseNumber", "CaseNumber") || "";
                const msg = pick(c, "commentText", "CommentText") || "";
                const role = pick(c, "authorRole", "AuthorRole") || "";
                const at = pick(c, "createdAt", "CreatedAt") || "";
                return (
                  "<li><span class=\"mono\">" +
                  cn +
                  "</span> <span class=\"muted\">(" +
                  role +
                  ")</span>: " +
                  msg +
                  ' <span class="muted">' +
                  formatDate(at) +
                  "</span></li>"
                );
              })
              .join("")
          : "<li class=\"muted\">نظری ثبت نشده است.</li>") +
        "</ul>";
    }
  }

  function renderFundCreditLimitsSection(section) {
    const host = qs("#dashFundCreditLimits");
    if (!host) return;

    if (!section) {
      host.classList.add("hidden");
      host.innerHTML = "";
      return;
    }

    const active = pick(section, "activePools", "ActivePools") || [];
    const historical = pick(section, "historicalPools", "HistoricalPools") || pick(section, "historical", "Historical") || [];

    host.classList.remove("hidden");
    host.innerHTML = '<div class="card__title">سقف اعتبار دوره‌ای صندوق</div>';

    const renderPoolCards = (pools, title) => {
      if (!pools.length) return "";
      let html = '<div class="fund-credit-dash__section"><div class="muted">' + title + "</div>";
      pools.forEach((p) => {
        const mod = pick(p, "moduleType", "ModuleType");
        const modLabel = Number(mod) === 2 ? "تسهیلات" : "ضمانت‌نامه";
        const budget = pick(p, "creditLimitWithCheck", "CreditLimitWithCheck");
        const used = pick(p, "totalUtilized", "TotalUtilized");
        const remaining = pick(p, "remainingCapacity", "RemainingCapacity");
        html +=
          '<div class="fund-credit-dash__card">' +
          "<strong>" +
          modLabel +
          "</strong> · " +
          (pick(p, "periodStart", "PeriodStart") || "—") +
          " تا " +
          (pick(p, "expiresAt", "ExpiresAt") || "—") +
          "<br/>بودجه: " +
          formatMoney(budget) +
          " · مصرف: " +
          formatMoney(used) +
          " · مانده: " +
          formatMoney(remaining) +
          "</div>";
      });
      return html + "</div>";
    };

    host.innerHTML +=
      renderPoolCards(active, "دوره‌های فعال") +
      renderPoolCards(historical, "تاریخچه تخصیص‌ها");
  }

  async function loadDashboard() {
    const errEl = qs("#dashAnalyticsError");
    const metaEl = qs("#dashAnalyticsMeta");
    errEl?.classList.add("hidden");
    destroyCharts();

    try {
      const res = await state.panel.apiRequest({ method: "GET", path: "/api/v1/dashboard/me" });
      const d = unwrap(res.body);
      if (!d) throw new Error("پاسخ داشبورد خالی است.");

      const viewType = pick(d, "viewType", "ViewType") || "Executive";
      const computedAt = pick(d, "computedAtUtc", "ComputedAtUtc");
      const isStale = pick(d, "isStale", "IsStale");

      if (metaEl) {
        metaEl.textContent =
          "نمای " +
          viewType +
          (computedAt ? " · به‌روزرسانی: " + formatDate(computedAt) : "") +
          (isStale ? " · (داده قدیمی)" : "");
      }

      showSection(viewType);

      if (viewType === "Executive") {
        renderExecutive(pick(d, "executive", "Executive") || {});
      } else if (viewType === "Department") {
        renderDepartment(pick(d, "department", "Department") || {});
      } else {
        renderApplicant(pick(d, "applicant", "Applicant") || {});
      }

      renderFundCreditLimitsSection(pick(d, "fundCreditLimits", "FundCreditLimits"));
    } catch (e) {
      if (errEl) {
        errEl.textContent = String(e.message || e);
        errEl.classList.remove("hidden");
      }
    }
  }

  function updateNav() {
    const role = resolveRole();
    const executive = ["CEO", "Admin", "InvestmentManager", "FinancialManager"].includes(role);
    const department = [
      "InvestmentExpert",
      "LegalExpert",
      "LegalManager",
      "FinancialExpert",
      "CreditExpert",
      "CreditManager",
      "TechnicalExpert",
      "TechnicalManager",
    ].includes(role);
    const applicant = role === "Applicant";
    const show = executive || department || applicant || role === "Admin";
    qs("#navDashboard")?.classList.toggle("hidden", !show);
  }

  function wire() {
    qs("#btnLoadAnalyticsDashboard")?.addEventListener("click", loadDashboard);
    document.addEventListener("testpanel:session-changed", () => {
      updateNav();
      if (qs("#tabDashboard")?.classList.contains("is-active")) loadDashboard();
    });

    const tabBtn = qs('[data-tab="tabDashboard"]');
    tabBtn?.addEventListener("click", () => {
      setTimeout(loadDashboard, 50);
    });
  }

  window.initDashboardAnalytics = function (panel) {
    state.panel = panel;
    wire();
    updateNav();
  };
})();
