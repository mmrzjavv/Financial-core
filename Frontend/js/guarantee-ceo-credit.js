/* global GuaranteeWorkflowModel */

(function () {

  const state = { panel: null, fundData: null, busy: false };



  const qs = (sel) => document.querySelector(sel);



  function pick(obj, camel, pascal) {

    if (!obj) return undefined;

    if (obj[camel] !== undefined) return obj[camel];

    if (obj[pascal] !== undefined) return obj[pascal];

    return undefined;

  }



  function unwrap(body) {

    return state.panel.unwrapEnvelope(body).payload;

  }



  function gPath(suffix) {

    return state.panel.guaranteeCasesBasePath() + suffix;

  }



  function resolveSessionRole(session) {

    if (!session) return "";

    if (window.WorkflowModel && typeof window.WorkflowModel.normalizeRole === "function") {

      return window.WorkflowModel.normalizeRole(session.userRoleText, session.userRoleNumber);

    }

    const role = String(session.userRoleText || "").trim();

    if (role) return role;

    const n = Number(session.userRoleNumber);

    if (n === 12) return "CEO";

    if (n === 100) return "Admin";

    return role;

  }



  function isCeoSession() {

    const session = state.panel.getActiveSession();

    const role = resolveSessionRole(session);

    return role === "CEO" || role === "Admin";

  }



  function formatRial(value) {

    if (value == null || value === "") return "—";

    const n = Number(value);

    if (!Number.isFinite(n)) return String(value);

    return n.toLocaleString("fa-IR") + " ریال";

  }



  function setError(msg) {

    const box = qs("#ceoCreditError");

    if (!box) return;

    box.classList.toggle("hidden", !msg);

    box.textContent = msg || "";

    if (msg) qs("#ceoCreditInfo")?.classList.add("hidden");

  }



  function setInfo(msg) {

    const box = qs("#ceoCreditInfo");

    if (!box) return;

    box.classList.toggle("hidden", !msg);

    box.textContent = msg || "";

    if (msg) qs("#ceoCreditError")?.classList.add("hidden");

  }



  function updateAccessUi() {

    const ceo = isCeoSession();

    const loggedIn = !!state.panel.getActiveSession()?.accessToken;

    const access = qs("#ceoGuaranteeCreditAccess");

    const panel = qs("#ceoGuaranteeCreditPanel");

    if (!loggedIn) {

      access?.classList.remove("hidden");

      if (access) access.textContent = "برای تعیین سقف اعتبار ابتدا از تب Auth وارد شوید.";

      panel?.classList.add("hidden");

      return;

    }

    if (!ceo) {

      access?.classList.remove("hidden");

      if (access) {

        access.innerHTML =

          "این بخش فقط با نقش <strong>مدیرعامل (CEO)</strong> در دسترس است. از تب Auth با نقش ۱۲ وارد شوید.";

      }

      panel?.classList.add("hidden");

      return;

    }

    access?.classList.add("hidden");

    panel?.classList.remove("hidden");

  }



  function renderSnapshotRows() {

    const host = qs("#ceoCreditSnapshotRows");

    if (!host) return;

    host.innerHTML = "";



    const d = state.fundData;

    const periodStart = pick(d, "periodStart", "PeriodStart");
    const expiresAt = pick(d, "expiresAt", "ExpiresAt");
    const periodLabel =
      periodStart && expiresAt ? periodStart + " تا " + expiresAt : "—";

    const rows = [
      ["بازه سقف فعال", periodLabel],
      ["سقف اعتبار کل صندوق (با چک)", formatRial(pick(d, "creditLimitWithCheck", "CreditLimitWithCheck"))],
      ["ضمانت‌نامه‌های صادره (در بازه)", formatRial(pick(d, "fundIssuedGuaranteesTotal", "FundIssuedGuaranteesTotal"))],
      ["تعهدات فعال (در بازه)", formatRial(pick(d, "activeCommitments", "ActiveCommitments"))],
      ["اعتبار باقی‌مانده", formatRial(pick(d, "remainingCredit", "RemainingCredit"))],
    ];



    rows.forEach(([label, value]) => {

      const row = document.createElement("div");

      row.className = "portal-profile-summary__row";

      const l = document.createElement("span");

      l.className = "portal-profile-summary__label muted";

      l.textContent = label;

      const v = document.createElement("span");

      v.className = "portal-profile-summary__value";

      v.textContent = value;

      row.appendChild(l);

      row.appendChild(v);

      host.appendChild(row);

    });

  }



  function renderSummary() {

    const detail = qs("#ceoCreditDetail");

    detail?.classList.remove("hidden");



    renderSnapshotRows();



    const limit = pick(state.fundData, "creditLimitWithCheck", "CreditLimitWithCheck");

    const currentEl = qs("#ceoCreditCurrentLimit");

    if (currentEl) {

      currentEl.textContent =

        limit > 0

          ? "سقف فعلی صندوق: " + formatRial(limit)

          : "هنوز سقفی برای کل صندوق ثبت نشده — مقدار جدید را وارد و ذخیره کنید.";

    }

    const input = qs("#ceoCreditLimitInput");

    if (input && limit > 0 && !input.value) input.value = String(limit);

  }



  async function loadFundCreditLimit() {

    setError("");

    const res = await state.panel.apiRequest({

      method: "GET",

      path: gPath("/fund-credit-limit"),

    });

    state.fundData = unwrap(res.body);

    const input = qs("#ceoCreditLimitInput");
    const ps = qs("#ceoCreditPeriodStart");
    const ex = qs("#ceoCreditExpiresAt");
    if (input) {
      const limit = pick(state.fundData, "creditLimitWithCheck", "CreditLimitWithCheck");
      input.value = limit > 0 ? String(limit) : "";
    }
    if (ps) ps.value = pick(state.fundData, "periodStart", "PeriodStart") || "";
    if (ex) ex.value = pick(state.fundData, "expiresAt", "ExpiresAt") || "";

    renderSummary();

    setInfo("وضعیت اعتباری کل صندوق بارگذاری شد.");

  }



  async function saveCreditLimit() {
    const raw = (qs("#ceoCreditLimitInput")?.value || "").trim();
    const amount = Number(raw);
    const periodStart = (qs("#ceoCreditPeriodStart")?.value || "").trim();
    const expiresAt = (qs("#ceoCreditExpiresAt")?.value || "").trim();
    if (!Number.isFinite(amount) || amount <= 0) throw new Error("مبلغ سقف اعتبار باید عددی بزرگ‌تر از صفر باشد.");
    if (!periodStart || !expiresAt) throw new Error("تاریخ شروع دوره و تاریخ انقضای سقف الزامی است.");
    if (expiresAt < periodStart) throw new Error("تاریخ انقضا باید بعد از تاریخ شروع دوره باشد.");

    if (state.busy) return;
    state.busy = true;
    try {
      const res = await state.panel.apiRequest({
        method: "PUT",
        path: gPath("/fund-credit-limit"),
        body: { creditLimitWithCheck: amount, periodStart, expiresAt },
      });

      state.fundData = unwrap(res.body);

      renderSummary();

      setInfo("سقف اعتبار کل صندوق ثبت شد و در همه پرونده‌های ضمانت‌نامه اعمال می‌شود.");

    } finally {

      state.busy = false;

    }

  }



  function wire() {

    qs("#ceoCreditLoadFund")?.addEventListener("click", async () => {

      try {

        await loadFundCreditLimit();

      } catch (e) {

        setError(e.message || String(e));

      }

    });



    qs("#ceoCreditSave")?.addEventListener("click", async () => {

      try {

        setError("");

        await saveCreditLimit();

      } catch (e) {

        setError(e.message || String(e));

      }

    });



    document.addEventListener("testpanel:session-changed", () => {

      updateAccessUi();

      if (!isCeoSession()) state.fundData = null;

    });



    document.querySelector('[data-tab="tabDashboard"]')?.addEventListener("click", () => {

      updateAccessUi();

      if (isCeoSession() && !state.fundData) {

        void loadFundCreditLimit().catch((e) => setError(e.message || String(e)));

      }

    });

  }



  window.initGuaranteeCeoCredit = function initGuaranteeCeoCredit(panel) {

    state.panel = panel;

    wire();

    updateAccessUi();

  };



  window.refreshGuaranteeCeoCreditAccess = updateAccessUi;

})();

