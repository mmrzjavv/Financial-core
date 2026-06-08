(function () {
  const CHART_COLORS = [
    "#6366f1", "#22c55e", "#f59e0b", "#ef4444", "#06b6d4",
    "#8b5cf6", "#ec4899", "#14b8a6", "#f97316", "#3b82f6",
  ];

  const MODULE_ORDER = ["Guarantee", "Investment", "Loan"];
  const MODULE_ICONS = { Guarantee: "🛡", Investment: "📈", Loan: "💳" };

  function pick(obj, ...keys) {
    if (!obj) return undefined;
    for (const k of keys) {
      if (obj[k] !== undefined && obj[k] !== null) return obj[k];
    }
    return undefined;
  }

  function formatMoney(n) {
    return (Number(n) || 0).toLocaleString("fa-IR") + " ریال";
  }

  function formatNum(n) {
    return (Number(n) || 0).toLocaleString("fa-IR");
  }

  function formatDate(iso) {
    if (!iso) return "—";
    try {
      return new Date(iso).toLocaleString("fa-IR");
    } catch {
      return String(iso);
    }
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

  function lineConfig(labels, data, label, color) {
    const c = color || "#6366f1";
    return {
      type: "line",
      data: {
        labels,
        datasets: [{
          label: label || "",
          data,
          borderColor: c,
          backgroundColor: c.replace(")", ",.15)").replace("rgb", "rgba").replace("#", ""),
          fill: true,
          tension: 0.35,
          pointRadius: 4,
          pointBackgroundColor: c,
        }],
      },
      options: {
        ...chartDefaults(),
        plugins: { ...chartDefaults().plugins, legend: { display: !!label, labels: { color: "#cbd5e1" } } },
        scales: {
          x: { ticks: { color: "#94a3b8" }, grid: { color: "rgba(255,255,255,0.05)" } },
          y: { ticks: { color: "#94a3b8" }, grid: { color: "rgba(255,255,255,0.05)" } },
        },
      },
    };
  }

  function fixLineConfig(config, color) {
    config.data.datasets[0].borderColor = color;
    config.data.datasets[0].pointBackgroundColor = color;
    config.data.datasets[0].backgroundColor = color + "26";
    return config;
  }

  function createChart(canvas, config, registry) {
    if (!canvas || typeof Chart === "undefined") return null;
    const chart = new Chart(canvas, config);
    if (registry) registry.push(chart);
    return chart;
  }

  function normalizeModules(modules) {
    const list = Array.isArray(modules) ? modules : [];
    return MODULE_ORDER.map((key) => list.find((m) => (pick(m, "module", "Module") || "") === key)).filter(Boolean);
  }

  function renderStatusTable(pipeline) {
    const rows = pipeline || [];
    if (!rows.length) return '<p class="muted">داده‌ای موجود نیست.</p>';
    return (
      '<table class="dashboard-status-table"><thead><tr><th>وضعیت</th><th>تعداد</th></tr></thead><tbody>' +
      rows
        .map((r) => {
          const title = pick(r, "statusTitle", "StatusTitle") || "";
          const count = pick(r, "count", "Count") || 0;
          return "<tr><td>" + title + '</td><td class="mono">' + formatNum(count) + "</td></tr>";
        })
        .join("") +
      "</tbody></table>"
    );
  }

  function renderDepartmentMetrics(metrics, departmentKey) {
    if (!metrics) return "";
    const key = (departmentKey || "").toLowerCase();
    const items = [];

    if (key === "financial") {
      items.push(
        { label: "کل کارمزد", value: formatMoney(pick(metrics, "totalCommissions", "TotalCommissions")) },
        { label: "بازپرداخت‌ها", value: formatMoney(pick(metrics, "totalRepayments", "TotalRepayments")) },
        { label: "اقساط معوق", value: formatNum(pick(metrics, "overdueInstallmentsCount", "OverdueInstallmentsCount")) },
        { label: "مبلغ معوق", value: formatMoney(pick(metrics, "overdueAmount", "OverdueAmount")) },
        { label: "بررسی مالی معلق", value: formatNum(pick(metrics, "pendingFinancialReviews", "PendingFinancialReviews")) }
      );
    } else if (key === "legal") {
      items.push(
        { label: "قرارداد در انتظار", value: formatNum(pick(metrics, "contractsPendingReview", "ContractsPendingReview")) },
        { label: "پرونده در فاز حقوقی", value: formatNum(pick(metrics, "casesInLegalPhase", "CasesInLegalPhase")) },
        { label: "آپلود قرارداد امضاشده", value: formatNum(pick(metrics, "pendingSignedContractUploads", "PendingSignedContractUploads")) }
      );
    } else if (key === "credit") {
      items.push(
        { label: "بررسی اعتبارات", value: formatNum(pick(metrics, "pendingCreditReviews", "PendingCreditReviews")) },
        { label: "بازگشت ۶ ماه", value: formatNum(pick(metrics, "revisionCountLast6Months", "RevisionCountLast6Months")) }
      );
    } else if (key === "investment") {
      items.push(
        { label: "ارزش‌گذاری معلق", value: formatNum(pick(metrics, "pendingValuations", "PendingValuations")) },
        { label: "در انتظار پرداخت", value: formatNum(pick(metrics, "waitingPaymentCount", "WaitingPaymentCount")) }
      );
    } else if (key === "technical") {
      items.push(
        { label: "سقف اعتبار فعال", value: formatNum(pick(metrics, "activeFundCreditPools", "ActiveFundCreditPools")) }
      );
    }

    if (!items.length) return "";
    return (
      '<div class="dept-metrics-strip">' +
      items
        .map(
          (it) =>
            '<div class="dept-metric"><span class="dept-metric__label">' +
            it.label +
            '</span><strong class="dept-metric__value">' +
            it.value +
            "</strong></div>"
        )
        .join("") +
      "</div>"
    );
  }

  function renderSystemHealth(health) {
    if (!health) return "";
    return (
      '<div class="system-health-strip">' +
      '<div class="system-health-card"><span class="muted">کاربران آنلاین</span><strong>' +
      formatNum(pick(health, "onlineUsersCount", "OnlineUsersCount")) +
      "</strong></div>" +
      '<div class="system-health-card"><span class="muted">فعال امروز</span><strong>' +
      formatNum(pick(health, "dailyActiveUsers", "DailyActiveUsers")) +
      "</strong></div>" +
      '<div class="system-health-card"><span class="muted">نشست‌های فعال</span><strong>' +
      formatNum(pick(health, "activeSessionsCount", "ActiveSessionsCount")) +
      "</strong></div></div>"
    );
  }

  function renderModuleSections(host, modules, chartRegistry, idPrefix) {
    if (!host) return;
    host.innerHTML = "";
    const ordered = normalizeModules(modules);
    if (!ordered.length) {
      host.innerHTML = '<p class="muted">داده ماژول‌ها موجود نیست.</p>';
      return;
    }

    const prefix = idPrefix || "modChart";
    ordered.forEach((mod, idx) => {
      const moduleKey = (pick(mod, "module", "Module") || "").toLowerCase();
      const title = pick(mod, "moduleTitle", "ModuleTitle") || moduleKey;
      const icon = MODULE_ICONS[pick(mod, "module", "Module")] || "📊";
      const trend = pick(mod, "monthlyTrend", "MonthlyTrend") || [];
      const pipeline = pick(mod, "pipelineByStatus", "PipelineByStatus") || [];

      const section = document.createElement("section");
      section.className = "module-section";
      section.dataset.module = moduleKey;
      section.innerHTML =
        '<div class="module-section__head"><span class="module-section__icon">' +
        icon +
        '</span><div><h3 class="module-section__title">' +
        title +
        '</h3><p class="muted module-section__sub">گزارش تفصیلی ماژول</p></div></div>' +
        '<div class="module-section__kpis">' +
        '<div class="module-kpi"><span>حجم فعال</span><strong>' +
        formatMoney(pick(mod, "activeVolume", "ActiveVolume")) +
        "</strong></div>" +
        '<div class="module-kpi"><span>پرونده فعال</span><strong>' +
        formatNum(pick(mod, "activeCases", "ActiveCases")) +
        "</strong></div>" +
        '<div class="module-kpi"><span>تکمیل‌شده</span><strong>' +
        formatNum(pick(mod, "completedCases", "CompletedCases")) +
        "</strong></div>" +
        '<div class="module-kpi"><span>نرخ تکمیل</span><strong>' +
        (pick(mod, "completionRate", "CompletionRate") || 0) +
        "%</strong></div>" +
        '<div class="module-kpi"><span>در انتظار CEO</span><strong>' +
        formatNum(pick(mod, "pendingCeoApprovals", "PendingCeoApprovals")) +
        "</strong></div>" +
        '<div class="module-kpi"><span>ردشده</span><strong>' +
        formatNum(pick(mod, "rejectedCount", "RejectedCount")) +
        "</strong></div>" +
        '<div class="module-kpi"><span>صف واحد</span><strong>' +
        formatNum(pick(mod, "queueCount", "QueueCount")) +
        "</strong></div></div>" +
        '<div class="module-section__body">' +
        '<div class="module-section__table"><div class="module-section__block-title">خط لوله وضعیت</div>' +
        renderStatusTable(pipeline) +
        '</div><div class="module-section__chart"><div class="module-section__block-title">روند ماهانه</div>' +
        '<div class="module-section__canvas"><canvas id="' +
        prefix +
        "_" +
        idx +
        '"></canvas></div></div></div>';

      host.appendChild(section);

      const labels = trend.map((m) => (pick(m, "year", "Year") || "") + "/" + (pick(m, "month", "Month") || ""));
      const data = trend.map((m) => pick(m, "count", "Count") || 0);
      if (labels.length && typeof Chart !== "undefined") {
        const canvas = document.getElementById(prefix + "_" + idx);
        const color = CHART_COLORS[idx % CHART_COLORS.length];
        createChart(canvas, fixLineConfig(lineConfig(labels, data, "پرونده جدید"), color), chartRegistry);
      }
    });
  }

  window.DashboardUi = {
    pick,
    formatMoney,
    formatNum,
    formatDate,
    normalizeModules,
    renderModuleSections,
    renderStatusTable,
    renderDepartmentMetrics,
    renderSystemHealth,
    createChart,
    lineConfig,
    CHART_COLORS,
  };
})();
