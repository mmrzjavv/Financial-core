(function () {
  const state = { panel: null, rows: [], busy: false };

  const MODULES = [
    { value: 1, label: "ضمانت‌نامه" },
    { value: 2, label: "تسهیلات" },
  ];

  function qs(sel) {
    return document.querySelector(sel);
  }

  function pick(obj, camel, pascal) {
    if (!obj) return undefined;
    if (obj[camel] !== undefined) return obj[camel];
    if (obj[pascal] !== undefined) return obj[pascal];
    return undefined;
  }

  function unwrap(body) {
    return state.panel.unwrapEnvelope(body).payload;
  }

  function apiPath(suffix) {
    return "/api/v" + (window.TESTPANEL_CONFIG?.casesVersion || "1") + "/fund-credit-limits" + (suffix || "");
  }

  function resolveSessionRole(session) {
    if (!session) return "";
    if (window.WorkflowModel && typeof window.WorkflowModel.normalizeRole === "function") {
      return window.WorkflowModel.normalizeRole(session.userRoleText, session.userRoleNumber);
    }
    return String(session.userRoleText || "").trim();
  }

  function canManageFundCreditLimits() {
    const role = resolveSessionRole(state.panel.getActiveSession());
    return ["CEO", "Admin", "TechnicalExpert"].includes(role);
  }

  function formatRial(value) {
    if (value == null || value === "") return "—";
    const n = Number(value);
    if (!Number.isFinite(n)) return String(value);
    return n.toLocaleString("fa-IR") + " ریال";
  }

  function moduleLabel(moduleType) {
    const n = Number(moduleType);
    if (n === 1) return "ضمانت‌نامه";
    if (n === 2) return "تسهیلات";
    return String(moduleType ?? "—");
  }

  function setError(msg) {
    const box = qs("#fundCreditLimitsError");
    if (!box) return;
    box.classList.toggle("hidden", !msg);
    box.textContent = msg || "";
    if (msg) qs("#fundCreditLimitsInfo")?.classList.add("hidden");
  }

  function setInfo(msg) {
    const box = qs("#fundCreditLimitsInfo");
    if (!box) return;
    box.classList.toggle("hidden", !msg);
    box.textContent = msg || "";
    if (msg) qs("#fundCreditLimitsError")?.classList.add("hidden");
  }

  function updateAccessUi() {
    const access = qs("#fundCreditLimitsAccess");
    const panel = qs("#fundCreditLimitsPanel");
    const loggedIn = !!state.panel.getActiveSession()?.accessToken;
    const allowed = canManageFundCreditLimits();

    if (!loggedIn) {
      access?.classList.remove("hidden");
      if (access) access.textContent = "برای مدیریت سقف اعتبار دوره‌ای ابتدا وارد شوید.";
      panel?.classList.add("hidden");
      return;
    }

    if (!allowed) {
      access?.classList.remove("hidden");
      if (access) access.textContent = "دسترسی به سقف اعتبار دوره‌ای فقط برای مدیرعامل، کارشناس فنی و مدیر سیستم مجاز است.";
      panel?.classList.add("hidden");
      return;
    }

    access?.classList.add("hidden");
    panel?.classList.remove("hidden");
  }

  function renderGrid() {
    const host = qs("#fundCreditLimitsGrid");
    if (!host) return;
    host.innerHTML = "";

    if (!state.rows.length) {
      host.appendChild(document.createTextNode("هنوز سقف دوره‌ای ثبت نشده است."));
      return;
    }

    const table = document.createElement("table");
    table.className = "fund-credit-grid";
    table.innerHTML =
      "<thead><tr>" +
      "<th>ماژول</th><th>شروع</th><th>پایان</th><th>سقف</th><th>مصرف</th><th>مانده</th><th>ثبت‌کننده</th>" +
      "</tr></thead>";
    const tbody = document.createElement("tbody");

    state.rows.forEach((row) => {
      const tr = document.createElement("tr");
      tr.innerHTML =
        "<td>" +
        moduleLabel(pick(row, "moduleType", "ModuleType")) +
        "</td><td>" +
        (pick(row, "periodStart", "PeriodStart") || "—") +
        "</td><td>" +
        (pick(row, "expiresAt", "ExpiresAt") || "—") +
        "</td><td>" +
        formatRial(pick(row, "creditLimitWithCheck", "CreditLimitWithCheck")) +
        "</td><td>" +
        formatRial(pick(row, "totalUtilized", "TotalUtilized")) +
        "</td><td>" +
        formatRial(pick(row, "remainingCapacity", "RemainingCapacity")) +
        "</td><td class=\"mono\">" +
        (pick(row, "lastSetByUserId", "LastSetByUserId") || "—") +
        "</td>";
      tbody.appendChild(tr);
    });

    table.appendChild(tbody);
    host.appendChild(table);
  }

  async function loadList() {
    setError("");
    const res = await state.panel.apiRequest({ method: "GET", path: apiPath("") });
    const payload = unwrap(res.body);
    state.rows = Array.isArray(payload) ? payload : pick(payload, "items", "Items") || [];
    renderGrid();
    setInfo("فهرست سقف‌های دوره‌ای بارگذاری شد.");
  }

  async function saveNewLimit() {
    const moduleType = Number(qs("#fundCreditModuleType")?.value || 0);
    const amount = Number(qs("#fundCreditAmount")?.value || 0);
    const periodStart = (qs("#fundCreditPeriodStart")?.value || "").trim();
    const expiresAt = (qs("#fundCreditExpiresAt")?.value || "").trim();

    if (!moduleType) throw new Error("نوع ماژول را انتخاب کنید.");
    if (!Number.isFinite(amount) || amount <= 0) throw new Error("مبلغ سقف باید عددی بزرگ‌تر از صفر باشد.");
    if (!periodStart || !expiresAt) throw new Error("تاریخ شروع و پایان دوره الزامی است.");
    if (expiresAt < periodStart) throw new Error("تاریخ پایان باید بعد از تاریخ شروع باشد.");

    if (state.busy) return;
    state.busy = true;
    try {
      await state.panel.apiRequest({
        method: "POST",
        path: apiPath(""),
        body: {
          moduleType,
          creditLimitWithCheck: amount,
          periodStart,
          expiresAt,
        },
      });
      await loadList();
      setInfo("سقف اعتبار دوره‌ای جدید ثبت شد.");
    } finally {
      state.busy = false;
    }
  }

  function wire() {
    qs("#fundCreditLoadList")?.addEventListener("click", () => {
      void loadList().catch((e) => setError(e.message || String(e)));
    });

    qs("#fundCreditSaveNew")?.addEventListener("click", () => {
      void saveNewLimit().catch((e) => setError(e.message || String(e)));
    });

    document.addEventListener("testpanel:session-changed", () => {
      updateAccessUi();
      if (!canManageFundCreditLimits()) state.rows = [];
    });

    document.querySelector('[data-tab="tabDashboard"]')?.addEventListener("click", () => {
      updateAccessUi();
      if (canManageFundCreditLimits() && !state.rows.length) {
        void loadList().catch((e) => setError(e.message || String(e)));
      }
    });
  }

  window.initFundCreditLimits = function initFundCreditLimits(panel) {
    state.panel = panel;
    const select = qs("#fundCreditModuleType");
    if (select && !select.options.length) {
      MODULES.forEach((m) => {
        const opt = document.createElement("option");
        opt.value = String(m.value);
        opt.textContent = m.label;
        select.appendChild(opt);
      });
    }
    wire();
    updateAccessUi();
  };

  window.refreshFundCreditLimitsAccess = updateAccessUi;
})();
