(function () {
  const ui = () => window.DashboardUi || {};
  const pick = (...args) => (ui().pick ? ui().pick(...args) : undefined);
  const formatNum = (n) => (ui().formatNum ? ui().formatNum(n) : String(n));

  const KPI_ROLES = ["Admin", "CEO", "BoardMember", "TechnicalExpert"];

  const PERIODS = [
    { id: "Last30Days", label: "۳۰ روز اخیر" },
    { id: "Last90Days", label: "۹۰ روز اخیر" },
    { id: "ThisQuarter", label: "فصل جاری" },
    { id: "AllTime", label: "کل دوره" },
  ];

  const state = {
    panel: null,
    period: "Last30Days",
    data: null,
    charts: [],
    loading: false,
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

  function canViewKpi() {
    const role = resolveRole();
    return KPI_ROLES.includes(role);
  }

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
        legend: { display: false },
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

  function formatRelativeTime(iso) {
    if (!iso) return "—";
    const then = new Date(iso).getTime();
    if (Number.isNaN(then)) return "—";
    const diffMs = Date.now() - then;
    const hours = Math.floor(diffMs / 3600000);
    if (hours < 1) return "کمتر از ۱ ساعت پیش";
    if (hours < 24) return hours + " ساعت پیش";
    const days = Math.floor(hours / 24);
    return days + " روز پیش";
  }

  function hoursToDaysLabel(hours) {
    const h = Number(hours) || 0;
    if (h >= 48) return (h / 24).toFixed(1) + " روز";
    return h.toFixed(1) + " ساعت";
  }

  function renderDepartmentSection(dept, idx) {
    const key = pick(dept, "departmentKey", "DepartmentKey") || "dept";
    const title = pick(dept, "departmentTitle", "DepartmentTitle") || key;
    const employees = pick(dept, "employees", "Employees") || [];
    if (!employees.length) return "";

    const rows = employees
      .map((emp) => {
        const name = pick(emp, "fullName", "FullName") || pick(emp, "userId", "UserId") || "—";
        const avg = pick(emp, "averageResolutionDays", "AverageResolutionDays") || 0;
        const min = pick(emp, "minResolutionHours", "MinResolutionHours") || 0;
        const max = pick(emp, "maxResolutionHours", "MaxResolutionHours") || 0;
        const total = pick(emp, "totalTasksResolved", "TotalTasksResolved") || 0;
        return (
          "<tr><td>" +
          name +
          "</td><td>" +
          avg +
          "</td><td>" +
          hoursToDaysLabel(min) +
          "</td><td>" +
          hoursToDaysLabel(max) +
          "</td><td>" +
          formatNum(total) +
          "</td></tr>"
        );
      })
      .join("");

    return (
      '<section class="kpi-dept-section" data-dept="' +
      key +
      '">' +
      '<div class="kpi-dept-section__head"><h3>' +
      title +
      '</h3><span class="muted">' +
      formatNum(employees.length) +
      " نفر</span></div>" +
      '<div class="kpi-dept-section__charts">' +
      '<div class="kpi-chart-card"><div class="kpi-chart-card__title">میانگین زمان رسیدگی (روز)</div>' +
      '<div class="kpi-chart-card__canvas"><canvas id="kpiBar_' +
      idx +
      '"></canvas></div></div>' +
      '<div class="kpi-chart-card"><div class="kpi-chart-card__title">پراکندگی زمان رسیدگی</div>' +
      '<div class="kpi-chart-card__canvas"><canvas id="kpiScatter_' +
      idx +
      '"></canvas></div></div>' +
      "</div>" +
      '<div class="kpi-table-wrap"><table class="kpi-table"><thead><tr>' +
      "<th>کارمند</th><th>میانگین (روز)</th><th>حداقل</th><th>حداکثر</th><th>تعداد اقدام</th>" +
      "</tr></thead><tbody>" +
      rows +
      "</tbody></table></div></section>"
    );
  }

  function renderCharts(departments) {
    destroyCharts();
    const colors = ui().CHART_COLORS || ["#6366f1", "#22c55e", "#f59e0b", "#ef4444", "#06b6d4"];

    departments.forEach((dept, idx) => {
      const employees = pick(dept, "employees", "Employees") || [];
      const labels = employees.map(
        (e) => pick(e, "fullName", "FullName") || pick(e, "userId", "UserId") || "—"
      );
      const avgDays = employees.map((e) => pick(e, "averageResolutionDays", "AverageResolutionDays") || 0);

      const barCanvas = document.getElementById("kpiBar_" + idx);
      if (barCanvas && labels.length) {
        createChart(barCanvas, {
          type: "bar",
          data: {
            labels,
            datasets: [
              {
                label: "میانگین (روز)",
                data: avgDays,
                backgroundColor: colors[idx % colors.length] + "cc",
                borderRadius: 6,
              },
            ],
          },
          options: {
            ...chartDefaults(),
            indexAxis: "y",
            scales: {
              x: { ticks: { color: "#94a3b8" }, grid: { color: "rgba(255,255,255,0.05)" } },
              y: { ticks: { color: "#94a3b8", font: { size: 11 } }, grid: { display: false } },
            },
          },
        });
      }

      const scatterPoints = [];
      employees.forEach((emp, empIdx) => {
        const name = pick(emp, "fullName", "FullName") || pick(emp, "userId", "UserId") || "—";
        const samples = pick(emp, "resolutionHoursSamples", "ResolutionHoursSamples") || [];
        samples.forEach((h) => {
          scatterPoints.push({ x: empIdx + 1, y: Number(h) / 24, label: name });
        });
      });

      const scatterCanvas = document.getElementById("kpiScatter_" + idx);
      if (scatterCanvas && scatterPoints.length) {
        createChart(scatterCanvas, {
          type: "scatter",
          data: {
            datasets: [
              {
                label: "نمونه رسیدگی (روز)",
                data: scatterPoints,
                backgroundColor: colors[idx % colors.length] + "aa",
                pointRadius: 4,
                pointHoverRadius: 6,
              },
            ],
          },
          options: {
            ...chartDefaults(),
            plugins: {
              ...chartDefaults().plugins,
              tooltip: {
                ...chartDefaults().plugins.tooltip,
                callbacks: {
                  label(ctx) {
                    const p = ctx.raw || {};
                    return (p.label || "") + ": " + (Number(p.y) || 0).toFixed(2) + " روز";
                  },
                },
              },
            },
            scales: {
              x: {
                type: "linear",
                ticks: {
                  color: "#94a3b8",
                  callback(v) {
                    const emp = employees[v - 1];
                    if (!emp) return "";
                    const n = pick(emp, "fullName", "FullName") || "";
                    return n.length > 10 ? n.slice(0, 10) + "…" : n;
                  },
                },
                grid: { color: "rgba(255,255,255,0.05)" },
              },
              y: {
                title: { display: true, text: "روز", color: "#94a3b8" },
                ticks: { color: "#94a3b8" },
                grid: { color: "rgba(255,255,255,0.05)" },
              },
            },
          },
        });
      }
    });
  }

  function setLoading(on) {
    state.loading = on;
    qs("#employeeKpiLoading")?.classList.toggle("hidden", !on);
    qs("#employeeKpiContent")?.classList.toggle("employee-kpi__content--loading", on);
  }

  function setError(msg) {
    const el = qs("#employeeKpiError");
    if (!el) return;
    if (msg) {
      el.textContent = msg;
      el.classList.remove("hidden");
    } else {
      el.textContent = "";
      el.classList.add("hidden");
    }
  }

  async function runKpiJob() {
    const meta = qs("#employeeKpiMeta");
    if (meta) meta.textContent = "در حال محاسبه KPI — لطفاً صبر کنید…";
    await state.panel.apiRequest({
      method: "POST",
      path: "/api/v1/analytics/employee-kpis/run-job",
    });
  }

  async function loadEmployeeKpis(forceRefresh) {
    if (!state.panel?.getActiveSession()?.accessToken) return;
    if (!canViewKpi()) return;

    setLoading(true);
    setError("");

    try {
      if (forceRefresh) await runKpiJob();

      const res = await state.panel.apiRequest({
        method: "GET",
        path: "/api/v1/analytics/employee-kpis?period=" + encodeURIComponent(state.period),
      });
      const raw = unwrap(res.body);
      state.data = raw;

      const computedAt = pick(raw, "computedAtUtc", "ComputedAtUtc");
      const isStale = !!pick(raw, "isStale", "IsStale");
      const meta = qs("#employeeKpiMeta");
      if (meta) {
        meta.textContent =
          "آخرین به‌روزرسانی: " +
          formatRelativeTime(computedAt) +
          (isStale ? " (داده قدیمی — در حال محاسبه مجدد)" : "");
      }

      const departments = pick(raw, "departments", "Departments") || [];
      const host = qs("#employeeKpiDepartments");
      if (host) {
        if (!departments.length) {
          host.innerHTML =
            '<p class="muted">داده KPI برای این بازه موجود نیست. برای محاسبه فوری روی «اجرای محاسبه» کلیک کنید یا منتظر job پس‌زمینه بمانید.</p>';
        } else {
          host.innerHTML = departments.map(renderDepartmentSection).join("");
          renderCharts(departments);
        }
      }
    } catch (err) {
      setError(err?.message || "بارگذاری KPI ناموفق بود.");
      qs("#employeeKpiDepartments").innerHTML = "";
      destroyCharts();
    } finally {
      setLoading(false);
    }
  }

  function switchSubTab(tab) {
    const overview = qs("#homeOverviewPanel");
    const kpi = qs("#homeEmployeeKpiPanel");
    const tabOverview = qs("#homeTabOverview");
    const tabKpi = qs("#homeTabEmployeeKpi");

    const showKpi = tab === "kpi";
    overview?.classList.toggle("hidden", showKpi);
    kpi?.classList.toggle("hidden", !showKpi);
    tabOverview?.classList.toggle("is-active", !showKpi);
    tabKpi?.classList.toggle("is-active", showKpi);

    if (showKpi && !state.data) void loadEmployeeKpis(false);
  }

  function updateVisibility() {
    const show = canViewKpi();
    qs("#homeTabEmployeeKpi")?.classList.toggle("hidden", !show);
    if (!show && qs("#homeEmployeeKpiPanel") && !qs("#homeEmployeeKpiPanel").classList.contains("hidden")) {
      switchSubTab("overview");
    }
  }

  function wire() {
    qs("#homeTabOverview")?.addEventListener("click", () => switchSubTab("overview"));
    qs("#homeTabEmployeeKpi")?.addEventListener("click", () => switchSubTab("kpi"));

    qs("#employeeKpiPeriod")?.addEventListener("change", (e) => {
      state.period = e.target.value || "Last30Days";
      state.data = null;
      if (!qs("#homeEmployeeKpiPanel")?.classList.contains("hidden")) {
        void loadEmployeeKpis(false);
      }
    });

    qs("#btnEmployeeKpiRefresh")?.addEventListener("click", () => {
      void loadEmployeeKpis(true);
    });

    document.addEventListener("testpanel:session-changed", () => {
      updateVisibility();
      state.data = null;
      if (!qs("#homeEmployeeKpiPanel")?.classList.contains("hidden")) {
        void loadEmployeeKpis(false);
      }
    });

    document.querySelector('[data-tab="tabDashboard"]')?.addEventListener("click", () => {
      updateVisibility();
    });
  }

  window.initEmployeeKpiDashboard = function initEmployeeKpiDashboard(panel) {
    state.panel = panel;
    wire();
    updateVisibility();
  };
})();
