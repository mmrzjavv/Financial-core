/* global TESTPANEL_CONFIG */
(function () {
  const LS_SESSIONS = "workflow_test_panel.sessions.v1";
  const LS_ACTIVE = "workflow_test_panel.active_session_id.v1";
  const LS_STATE = "workflow_test_panel.state.v1";

  const ROLE_OPTIONS = [
    { value: "", label: "(no change)" },
    { value: "1", label: "User (1) - Applicant-ish" },
    { value: "10", label: "InvestmentExpert (10)" },
    { value: "11", label: "InvestmentManager (11)" },
    { value: "12", label: "CEO (12)" },
    { value: "20", label: "LegalUnit (20)" },
    { value: "30", label: "FinancialUnit (30)" },
    { value: "40", label: "TechnicalExpert (40)" },
    { value: "100", label: "Admin (100)" },
  ];

  const CASE_PHASES = [
    { value: "", label: "(any)" },
    { value: "1", label: "Application (1)" },
    { value: "2", label: "Valuation (2)" },
    { value: "3", label: "Legal (3)" },
    { value: "4", label: "Finance (4)" },
    { value: "5", label: "Closing (5)" },
  ];

  const CASE_STATUSES = [
    { value: "", label: "(any)" },
    { value: "1", label: "Draft (1)" },
    { value: "2", label: "DataEntry1 (2)" },
    { value: "3", label: "ReviewDataEntry1 (3)" },
    { value: "4", label: "DataEntry2 (4)" },
    { value: "5", label: "ReviewDataEntry2 (5)" },
    { value: "6", label: "InitialValuation (6)" },
    { value: "7", label: "SecondaryValuation (7)" },
    { value: "8", label: "WaitingPreliminaryContract (8)" },
    { value: "9", label: "WaitingUserReviewPreliminaryContract (9)" },
    { value: "10", label: "ContractDrafting (10)" },
    { value: "11", label: "WaitingContractSignature (11)" },
    { value: "12", label: "WaitingSignedContractUpload (12)" },
    { value: "13", label: "WaitingFinancialWorksheet (13)" },
    { value: "14", label: "FinancialWorksheetReview (14)" },
    { value: "15", label: "WaitingPayment (15)" },
    { value: "16", label: "Completed (16)" },
    { value: "17", label: "Rejected (17)" },
    { value: "18", label: "Cancelled (18)" },
    { value: "19", label: "Archived (19)" },
  ];

  const DOC_TYPES = [
    { value: "1", label: "PitchDeck (1)" },
    { value: "2", label: "FinancialStatements (2)" },
    { value: "3", label: "TaxDocuments (3)" },
    { value: "4", label: "CompanyRegistration (4)" },
    { value: "5", label: "ShareholderManager (5)" },
    { value: "6", label: "SalesDocuments (6)" },
    { value: "7", label: "PreContract (7)" },
    { value: "8", label: "FinalContract (8)" },
    { value: "9", label: "SignedContract (9)" },
    { value: "10", label: "PaymentReceipt (10)" },
    { value: "99", label: "Other (99)" },
  ];

  const qs = (sel, root) => (root || document).querySelector(sel);
  const qsa = (sel, root) => Array.from((root || document).querySelectorAll(sel));

  function nowIso() {
    return new Date().toISOString();
  }

  function safeJsonParse(raw, fallback) {
    try {
      if (raw === "" || raw == null) return fallback;
      return JSON.parse(raw);
    } catch {
      return fallback;
    }
  }

  function pretty(obj) {
    if (obj == null) return "";
    try {
      return typeof obj === "string" ? obj : JSON.stringify(obj, null, 2);
    } catch {
      return String(obj);
    }
  }

  function unwrapEnvelope(body) {
    if (!body || typeof body !== "object") {
      return { envelope: null, payload: body };
    }

    if (Object.prototype.hasOwnProperty.call(body, "success")) {
      if (body.success === false) {
        const validation = Array.isArray(body.validationErrors) ? body.validationErrors.join(", ") : "";
        const message = body.message || "درخواست ناموفق بود";
        throw new Error(validation ? message + ": " + validation : message);
      }

      return {
        envelope: body,
        payload: body.data !== undefined && body.data !== null
          ? body.data
          : body.list !== undefined
            ? body.list
            : body,
      };
    }

    return { envelope: null, payload: body };
  }

  function setGlobalError(message) {
    const box = qs("#globalError");
    if (!message) {
      box.classList.add("hidden");
      box.textContent = "";
      return;
    }
    box.classList.remove("hidden");
    box.textContent = message;
  }

  function loadSessions() {
    return safeJsonParse(localStorage.getItem(LS_SESSIONS), []);
  }

  function saveSessions(list) {
    localStorage.setItem(LS_SESSIONS, JSON.stringify(list || []));
  }

  function getActiveSessionId() {
    return localStorage.getItem(LS_ACTIVE) || "";
  }

  function setActiveSessionId(id) {
    if (!id) localStorage.removeItem(LS_ACTIVE);
    else localStorage.setItem(LS_ACTIVE, id);
  }

  function getActiveSession() {
    const sessions = loadSessions();
    const activeId = getActiveSessionId();
    return sessions.find((s) => s.id === activeId) || null;
  }

  function findSessionByPhone(phone) {
    return loadSessions().find((session) => session.phone === phone) || null;
  }

  function saveState(partial) {
    const prev = safeJsonParse(localStorage.getItem(LS_STATE), {});
    const next = { ...prev, ...(partial || {}) };
    localStorage.setItem(LS_STATE, JSON.stringify(next));
    return next;
  }

  function loadState() {
    return safeJsonParse(localStorage.getItem(LS_STATE), {});
  }

  function optFill(selectEl, options) {
    selectEl.innerHTML = "";
    for (const opt of options) {
      const o = document.createElement("option");
      o.value = opt.value;
      o.textContent = opt.label;
      selectEl.appendChild(o);
    }
  }

  function makeUrl(pathOrUrl) {
    const baseUrl = TESTPANEL_CONFIG.baseUrl;
    const p = String(pathOrUrl || "").trim();
    if (!p) return baseUrl;
    if (/^https?:\/\//i.test(p)) return p;
    if (!p.startsWith("/")) return baseUrl + "/" + p;
    return baseUrl + p;
  }

  function casesBasePath() {
    return "/api/v" + TESTPANEL_CONFIG.casesVersion + "/cases";
  }

  const inspector = {
    lastRequest: null,
    lastResponse: null,
    lastStatus: "",
    lastTime: "",
    log: [],
    maxLog: 50,
  };

  function renderInspector() {
    qs("#lastRequest").textContent = pretty(inspector.lastRequest) || "";
    qs("#lastResponse").textContent = pretty(inspector.lastResponse) || "";
    qs("#lastStatus").textContent = inspector.lastStatus || "";
    qs("#lastTime").textContent = inspector.lastTime || "";

    const logRoot = qs("#reqLog");
    logRoot.innerHTML = "";
    for (const item of inspector.log) {
      const div = document.createElement("div");
      div.className = "listitem";
      const main = document.createElement("div");
      main.className = "listitem__main";
      const title = document.createElement("div");
      title.className = "mono";
      title.textContent = item.method + " " + item.path;
      const sub = document.createElement("div");
      sub.className = "muted";
      sub.textContent =
        item.time + " • " + item.status + " • " + item.durationMs + "ms";
      main.appendChild(title);
      main.appendChild(sub);

      const actions = document.createElement("div");
      actions.className = "listitem__actions";
      const btn = document.createElement("button");
      btn.className = "btn btn--small";
      btn.textContent = "Inspect";
      btn.addEventListener("click", () => {
        inspector.lastRequest = item.request;
        inspector.lastResponse = item.response;
        inspector.lastStatus = item.status;
        inspector.lastTime = item.durationMs + "ms";
        renderInspector();
      });
      actions.appendChild(btn);

      div.appendChild(main);
      div.appendChild(actions);
      logRoot.appendChild(div);
    }
  }

  async function apiRequest(opts) {
    const method = (opts.method || "GET").toUpperCase();
    const path = opts.path || "";
    const url = makeUrl(path);
    const useAuth = opts.useAuth !== false;

    const headers = { ...(opts.headers || {}) };
    const session = getActiveSession();
    if (useAuth && session && session.accessToken) {
      headers.Authorization = "Bearer " + session.accessToken;
    }

    let body = opts.body;
    let bodyForLog = body;
    if (body != null && opts.json !== false && typeof body !== "string") {
      headers["Content-Type"] = headers["Content-Type"] || "application/json";
      body = JSON.stringify(body);
    }

    const requestForInspector = {
      at: nowIso(),
      method,
      url,
      headers,
      body: bodyForLog == null ? null : bodyForLog,
    };
    inspector.lastRequest = requestForInspector;
    inspector.lastResponse = null;
    inspector.lastStatus = "";
    inspector.lastTime = "";
    renderInspector();

    const start = performance.now();
    let res;
    let text = "";
    try {
      res = await fetch(url, {
        method,
        headers,
        body: body == null ? undefined : body,
      });
      text = await res.text();
    } catch (e) {
      const durationMs = Math.round(performance.now() - start);
      inspector.lastResponse = { networkError: true, message: String(e) };
      inspector.lastStatus = "NETWORK_ERROR";
      inspector.lastTime = durationMs + "ms";
      inspector.log.unshift({
        time: nowIso(),
        method,
        path,
        status: "NETWORK_ERROR",
        durationMs,
        request: requestForInspector,
        response: inspector.lastResponse,
      });
      inspector.log = inspector.log.slice(0, inspector.maxLog);
      renderInspector();
      throw e;
    }

    const durationMs = Math.round(performance.now() - start);
    const contentType = (res.headers.get("content-type") || "").toLowerCase();
    const parsed = contentType.includes("application/json")
      ? safeJsonParse(text, { raw: text })
      : { raw: text };

    const responseForInspector = {
      ok: res.ok,
      status: res.status,
      statusText: res.statusText,
      headers: Object.fromEntries(res.headers.entries()),
      body: parsed,
    };

    inspector.lastResponse = responseForInspector;
    inspector.lastStatus = String(res.status);
    inspector.lastTime = durationMs + "ms";
    inspector.log.unshift({
      time: nowIso(),
      method,
      path,
      status: String(res.status),
      durationMs,
      request: requestForInspector,
      response: responseForInspector,
    });
    inspector.log = inspector.log.slice(0, inspector.maxLog);
    renderInspector();

    if (parsed && typeof parsed === "object" && parsed.success === false) {
      const validation = Array.isArray(parsed.validationErrors) ? parsed.validationErrors.join(", ") : "";
      const message = parsed.message || "درخواست ناموفق بود";
      throw new Error(validation ? message + ": " + validation : message);
    }

    return {
      ok: res.ok,
      status: res.status,
      body: parsed,
      text,
      durationMs,
      headers: res.headers,
    };
  }

  function requireCaseId() {
    const st = loadState();
    const id = (st.currentCaseId || "").trim();
    if (!id) throw new Error("شناسه پرونده جاری تنظیم نشده است.");
    return id;
  }

  function setCurrentCaseId(id) {
    const v = String(id || "").trim();
    saveState({ currentCaseId: v });
    qs("#currentCaseId").value = v;
    qs("#currentCaseLabel").textContent = v || "(not set)";
    document.dispatchEvent(new CustomEvent("testpanel:case-changed", { detail: { caseId: v } }));
  }

  function renderTopbar() {
    qs("#baseUrlLabel").textContent =
      TESTPANEL_CONFIG.baseUrl + " (cases v" + TESTPANEL_CONFIG.casesVersion + ")";
    const s = getActiveSession();
    qs("#activeSessionLabel").textContent = s
      ? (s.label || s.phone || s.id) + " • " + (s.userRoleText || "role?")
      : "(none)";
  }

  function renderSessionsList() {
    const root = qs("#sessionsList");
    const sessions = loadSessions();
    const active = getActiveSessionId();
    root.innerHTML = "";
    if (!sessions.length) {
      const empty = document.createElement("div");
      empty.className = "muted";
      empty.textContent = "No saved sessions yet.";
      root.appendChild(empty);
      return;
    }
    for (const s of sessions) {
      const item = document.createElement("div");
      item.className = "listitem";

      const main = document.createElement("div");
      main.className = "listitem__main";
      const title = document.createElement("div");
      title.className = "mono";
      title.textContent =
        (s.label || "(no label)") +
        " • " +
        (s.userRoleText || "role?") +
        (s.id === active ? " • ACTIVE" : "");
      const sub = document.createElement("div");
      sub.className = "muted";
      sub.textContent =
        "phone=" +
        (s.phone || "?") +
        " • userId=" +
        (s.userId || "?") +
        " • savedAt=" +
        (s.savedAt || "?");
      main.appendChild(title);
      main.appendChild(sub);

      const actions = document.createElement("div");
      actions.className = "listitem__actions";

      const btnUse = document.createElement("button");
      btnUse.className = "btn btn--small btn--primary";
      btnUse.textContent = "Use";
      btnUse.addEventListener("click", () => {
        setActiveSessionId(s.id);
        renderSessionsList();
        renderTopbar();
      });
      actions.appendChild(btnUse);

      const btnDel = document.createElement("button");
      btnDel.className = "btn btn--small btn--warn";
      btnDel.textContent = "Delete";
      btnDel.addEventListener("click", () => {
        const next = loadSessions().filter((x) => x.id !== s.id);
        saveSessions(next);
        if (getActiveSessionId() === s.id) setActiveSessionId(next[0]?.id || "");
        renderSessionsList();
        renderTopbar();
      });
      actions.appendChild(btnDel);

      item.appendChild(main);
      item.appendChild(actions);
      root.appendChild(item);
    }
  }

  function wireTabs() {
    qsa(".navbtn").forEach((b) => {
      b.addEventListener("click", () => {
        qsa(".navbtn").forEach((x) => x.classList.remove("is-active"));
        b.classList.add("is-active");
        const target = b.getAttribute("data-tab");
        qsa(".tab").forEach((t) => t.classList.remove("is-active"));
        qs("#" + target).classList.add("is-active");
        setGlobalError("");
      });
    });
  }

  function fillSelects() {
    optFill(qs("#updateUserRole"), ROLE_OPTIONS);
    optFill(qs("#searchPhase"), CASE_PHASES);
    optFill(qs("#searchStatus"), CASE_STATUSES);
    optFill(qs("#docType"), DOC_TYPES);
  }

  function setDefaultJsonTemplates() {
    qs("#dataEntry1Json").value = pretty({
      startupTitle: "My Startup",
      businessDescription: "Short description",
      requestedAmount: 100000000,
      teamSize: 3,
      website: "https://example.com",
      country: "IR",
      city: "Tehran",
    });

    qs("#dataEntry2Json").value = pretty({
      marketAnalysis: "Market analysis ...",
      revenueModel: "Revenue model ...",
      competitiveAdvantage: "Competitive advantage ...",
      financialProjection: "Optional",
    });

    qs("#valuationJson").value = pretty({
      type: 1,
      amount: 2500000000,
      notes: "Primary valuation",
    });

    qs("#worksheetJson").value = pretty({
      bankName: "Bank",
      iban: "IR000000000000000000000000",
      approvedAmount: 100000000,
      paymentSchedule: "milestone-based",
      notes: "worksheet notes",
    });

    const today = new Date();
    const yyyy = today.getFullYear();
    const mm = String(today.getMonth() + 1).padStart(2, "0");
    const dd = String(today.getDate()).padStart(2, "0");
    const isoDate = yyyy + "-" + mm + "-" + dd;
    qs("#paymentJson").value = pretty({
      amount: 50000000,
      paymentDate: isoDate,
      transactionNumber: "TX-" + Date.now(),
      receiptS3Key: null,
      notes: "payment notes",
      method: 1,
      status: 1,
    });

    qs("#commentJson").value = pretty({
      phase: 1,
      message: "Comment message",
      isInternal: false,
      parentId: null,
    });

    qs("#evalJson").value = pretty({
      phase: 1,
      notes: "evaluation notes",
      items: [
        { title: "KYC", isApproved: true, comment: "ok" },
        { title: "Documents", isApproved: false, comment: "missing doc X" },
      ],
    });
  }

  function pickFileMime(fileInput, mimeInput) {
    const f = fileInput.files && fileInput.files[0];
    if (f && !mimeInput.value) mimeInput.value = f.type || "application/octet-stream";
    return f || null;
  }

  function saveSessionFromLogin(phone, label, loginBody) {
    const unwrapped = unwrapEnvelope(loginBody);
    const data = unwrapped.payload && (unwrapped.payload.tokenModel || unwrapped.payload.user)
      ? unwrapped.payload
      : loginBody && (loginBody.data || loginBody.Data || loginBody.value || loginBody.Value);
    const tokenModel = data && (data.tokenModel || data.TokenModel);
    const user = data && (data.user || data.User);
    const id = crypto && crypto.randomUUID ? crypto.randomUUID() : String(Date.now());

    const roleNumber = user && (user.roleNumber ?? user.RoleNumber);
    const roleText = user && (user.role ?? user.Role);
    const session = {
      id,
      label: (label || "").trim() || (roleText ? String(roleText) : "session") + " - " + phone,
      phone,
      userId: user && (user.id || user.Id),
      userRoleText: roleText != null ? String(roleText) : "",
      userRoleNumber: roleNumber != null ? Number(roleNumber) : null,
      accessToken: tokenModel && (tokenModel.accessToken || tokenModel.AccessToken),
      accessTokenExpiration: tokenModel && (tokenModel.accessTokenExpiration || tokenModel.AccessTokenExpiration),
      refreshToken: tokenModel && (tokenModel.refreshToken || tokenModel.RefreshToken),
      refreshTokenExpiration: tokenModel && (tokenModel.refreshTokenExpiration || tokenModel.RefreshTokenExpiration),
      raw: data || null,
      savedAt: nowIso(),
    };

    if (!session.accessToken || !session.refreshToken) {
      throw new Error("پاسخ ورود شامل توکن نیست. تب اشکال‌زدایی را بررسی کنید.");
    }

    const sessions = loadSessions();
    sessions.unshift(session);
    saveSessions(sessions);
    setActiveSessionId(session.id);
    document.dispatchEvent(new CustomEvent("testpanel:session-changed"));
    renderSessionsList();
    renderTopbar();
  }

  async function withUiError(fn) {
    try {
      setGlobalError("");
      await fn();
    } catch (e) {
      setGlobalError(String(e && e.message ? e.message : e));
    }
  }

  function parseJsonTextarea(id) {
    const raw = (qs(id).value || "").trim();
    if (!raw) return null;
    const v = safeJsonParse(raw, null);
    if (v == null) throw new Error("JSON نامعتبر در " + id);
    return v;
  }

  function buildQuery(params) {
    const sp = new URLSearchParams();
    for (const [k, v] of Object.entries(params || {})) {
      if (v === "" || v == null) continue;
      sp.set(k, String(v));
    }
    const s = sp.toString();
    return s ? "?" + s : "";
  }

  function wireConfigModal() {
    const modal = qs("#configModal");
    const open = () => {
      qs("#cfgBaseUrl").value = TESTPANEL_CONFIG.baseUrl;
      qs("#cfgCasesVersion").value = TESTPANEL_CONFIG.casesVersion;
      qs("#cfgDevOtp").value = TESTPANEL_CONFIG.devOtp;
      modal.classList.remove("hidden");
    };
    const close = () => modal.classList.add("hidden");

    qs("#btnEditConfig").addEventListener("click", open);
    qs("#btnCloseConfig").addEventListener("click", close);
    modal.addEventListener("click", (e) => {
      if (e.target === modal) close();
    });

    qs("#btnSaveConfig").addEventListener("click", () => {
      TESTPANEL_CONFIG.save({
        baseUrl: qs("#cfgBaseUrl").value,
        casesVersion: qs("#cfgCasesVersion").value,
        devOtp: qs("#cfgDevOtp").value,
      });
    });
  }

  function wireAuth() {
    qs("#btnSendOtp").addEventListener("click", () =>
      withUiError(async () => {
        const phone = qs("#authPhone").value.trim();
        if (!phone) throw new Error("شماره موبایل الزامی است.");
        await apiRequest({
          method: "POST",
          path: "/api/v1/panel/users/send-otp",
          useAuth: false,
          body: { phoneNumber: phone },
        });
        qs("#verifyPhone").value = phone;
      })
    );

    qs("#btnVerifyOtp").addEventListener("click", () =>
      withUiError(async () => {
        const phone = qs("#verifyPhone").value.trim();
        const otp = qs("#verifyOtp").value.trim();
        const label = qs("#sessionLabel").value.trim();
        if (!phone || !otp) throw new Error("شماره موبایل و کد تایید الزامی هستند.");

        const res = await apiRequest({
          method: "POST",
          path: "/api/v1/panel/users/verify-otp",
          useAuth: false,
          body: { phoneNumber: phone, otpCode: otp },
        });
        saveSessionFromLogin(phone, label, res.body);
      })
    );

    qs("#btnRefresh").addEventListener("click", () =>
      withUiError(async () => {
        const session = getActiveSession();
        if (!session) throw new Error("نشست فعالی وجود ندارد.");
        const override = qs("#refreshTokenInput").value.trim();
        const refreshToken = override || session.refreshToken;
        if (!refreshToken) throw new Error("توکن تازه‌سازی موجود نیست.");

        const res = await apiRequest({
          method: "POST",
          path: "/api/v1/panel/users/refresh-token",
          useAuth: false, // endpoint is [AllowAnonymous] but requires bearer access token header
          headers: { Authorization: "Bearer " + session.accessToken },
          body: { refreshToken },
        });

        // same LoginDto wrapper
        // refresh result might return LoginDto as Data
        saveSessionFromLogin(session.phone || "", (session.label || "") + " (refreshed)", res.body);
      })
    );

    qs("#btnLogout").addEventListener("click", () =>
      withUiError(async () => {
        const session = getActiveSession();
        if (!session) {
          setActiveSessionId("");
          renderTopbar();
          return;
        }
        await apiRequest({ method: "POST", path: "/api/v1/panel/users/logout" });
        // keep saved session but deactivate
        setActiveSessionId("");
        renderTopbar();
      })
    );

    qs("#btnProfile").addEventListener("click", () =>
      withUiError(async () => {
        await apiRequest({ method: "GET", path: "/api/v1/panel/users/profile" });
      })
    );

    qs("#btnSessions").addEventListener("click", () =>
      withUiError(async () => {
        await apiRequest({ method: "GET", path: "/api/v1/panel/users/sessions" });
      })
    );

    qs("#btnRevokeSession").addEventListener("click", () =>
      withUiError(async () => {
        const sid = qs("#revokeSid").value.trim();
        if (!sid) throw new Error("شناسه نشست (sid) الزامی است.");
        await apiRequest({
          method: "POST",
          path: "/api/v1/panel/users/sessions/revoke",
          body: { sessionId: sid },
        });
      })
    );

    qs("#btnRevokeAll").addEventListener("click", () =>
      withUiError(async () => {
        await apiRequest({ method: "POST", path: "/api/v1/panel/users/sessions/revoke-all" });
      })
    );
  }

  function wireUsers() {
    qs("#btnCreateUser").addEventListener("click", () =>
      withUiError(async () => {
        const dto = {
          phoneNumber: qs("#createUserPhone").value.trim(),
          email: qs("#createUserEmail").value.trim() || null,
          firstName: qs("#createUserFirst").value.trim(),
          lastName: qs("#createUserLast").value.trim(),
          nationalCode: qs("#createUserNat").value.trim() || null,
        };
        if (!dto.phoneNumber) throw new Error("شماره موبایل الزامی است.");
        if (!dto.firstName || !dto.lastName) throw new Error("نام و نام خانوادگی الزامی هستند.");
        await apiRequest({ method: "POST", path: "/api/v1/panel/users", useAuth: false, body: dto });
      })
    );

    qs("#btnUpdateUser").addEventListener("click", () =>
      withUiError(async () => {
        const id = qs("#updateUserId").value.trim();
        if (!id) throw new Error("شناسه کاربر الزامی است.");
        const roleRaw = qs("#updateUserRole").value;
        const isActiveRaw = qs("#updateUserActive").value;
        const dto = {};
        if (roleRaw !== "") dto.role = Number(roleRaw);
        if (isActiveRaw !== "") dto.isActive = isActiveRaw === "true";
        await apiRequest({ method: "PUT", path: "/api/v1/panel/users/" + encodeURIComponent(id), body: dto });
      })
    );

    qs("#btnGetUser").addEventListener("click", () =>
      withUiError(async () => {
        const id = qs("#getUserId").value.trim();
        if (!id) throw new Error("شناسه کاربر الزامی است.");
        await apiRequest({ method: "GET", path: "/api/v1/panel/users/" + encodeURIComponent(id) });
      })
    );

    qs("#btnListUsers").addEventListener("click", () =>
      withUiError(async () => {
        const take = qs("#usersTake").value.trim() || "10";
        const skip = qs("#usersSkip").value.trim() || "0";
        await apiRequest({ method: "GET", path: "/api/v1/panel/users?take=" + encodeURIComponent(take) + "&skip=" + encodeURIComponent(skip) });
      })
    );
  }

  function optionalText(selector) {
    const value = qs(selector).value.trim();
    return value || null;
  }

  function populateCompanySelect(companies) {
    const select = qs("#caseCompanyId");
    select.innerHTML = "";
    if (!companies || !companies.length) {
      const option = document.createElement("option");
      option.value = "";
      option.textContent = "شرکتی ثبت نشده است";
      select.appendChild(option);
      return;
    }

    companies.forEach((company) => {
      const option = document.createElement("option");
      const id = company.id || company.Id;
      const name = company.name || company.Name || "شرکت";
      const economicCode = company.economicCode || company.EconomicCode || "";
      option.value = id;
      option.textContent = economicCode ? name + " (" + economicCode + ")" : name;
      select.appendChild(option);
    });
  }

  function syncCaseCompanyFields() {
    const isCompany = Number(qs("#caseApplicantType").value) === 2;
    qs("#caseCompanyRow").style.display = isCompany ? "" : "none";
  }

  async function loadMyCompanies() {
    const res = await apiRequest({ method: "GET", path: "/api/v1/panel/companies/mine" });
    const companies = unwrapEnvelope(res.body).payload || [];
    populateCompanySelect(companies);
    return companies;
  }

  function buildCompanyPayload() {
    return {
      name: qs("#companyName").value.trim(),
      economicCode: qs("#companyEconomicCode").value.trim(),
      registrationNumber: optionalText("#companyRegistrationNumber"),
      nationalId: optionalText("#companyNationalId"),
      phoneNumber: optionalText("#companyPhoneNumber"),
      address: optionalText("#companyAddress"),
      city: optionalText("#companyCity"),
      province: optionalText("#companyProvince"),
      postalCode: optionalText("#companyPostalCode"),
    };
  }

  function wireCases() {
    qs("#btnSetCurrentCase").addEventListener("click", () =>
      withUiError(async () => {
        setCurrentCaseId(qs("#currentCaseId").value);
      })
    );

    qs("#caseApplicantType").addEventListener("change", syncCaseCompanyFields);
    syncCaseCompanyFields();

    qs("#btnLoadCompanies").addEventListener("click", () =>
      withUiError(async () => {
        await loadMyCompanies();
      })
    );

    qs("#btnCreateCompany").addEventListener("click", () =>
      withUiError(async () => {
        const payload = buildCompanyPayload();
        if (!payload.name || !payload.economicCode) {
          throw new Error("نام شرکت و کد اقتصادی الزامی است.");
        }

        await apiRequest({ method: "POST", path: "/api/v1/panel/companies", body: payload });
        await loadMyCompanies();
      })
    );

    qs("#btnCreateCase").addEventListener("click", () =>
      withUiError(async () => {
        const applicantType = Number(qs("#caseApplicantType").value);
        const payload = { applicantType };

        if (applicantType === 2) {
          const companyId = qs("#caseCompanyId").value.trim();
          if (!companyId) throw new Error("برای متقاضی حقوقی، انتخاب شرکت الزامی است.");
          payload.companyId = companyId;
        }

        const res = await apiRequest({ method: "POST", path: casesBasePath(), body: payload });
        const created = unwrapEnvelope(res.body).payload;
        const caseId = created && (created.id || created.Id);
        if (caseId) setCurrentCaseId(caseId);
      })
    );

    qs("#btnGetCase").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        await apiRequest({ method: "GET", path: casesBasePath() + "/" + id });
      })
    );

    qs("#btnGetHistory").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        await apiRequest({ method: "GET", path: casesBasePath() + "/" + id + "/history" });
      })
    );

    qs("#btnSearchCases").addEventListener("click", () =>
      withUiError(async () => {
        const query = buildQuery({
          caseNumber: qs("#searchCaseNumber").value.trim(),
          applicantUserId: qs("#searchApplicantUserId").value.trim(),
          phase: qs("#searchPhase").value,
          status: qs("#searchStatus").value,
          fromDate: qs("#searchFrom").value.trim(),
          toDate: qs("#searchTo").value.trim(),
          page: qs("#searchPage").value.trim(),
          pageSize: qs("#searchPageSize").value.trim(),
        });
        await apiRequest({ method: "GET", path: casesBasePath() + query });
      })
    );
  }

  function wireWorkflow() {
    qs("#btnReloadCase").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        await apiRequest({ method: "GET", path: casesBasePath() + "/" + id });
      })
    );

    // Data entry 1
    qs("#btnUpdateDE1").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const payload = parseJsonTextarea("#dataEntry1Json");
        await apiRequest({ method: "PUT", path: casesBasePath() + "/" + id + "/data-entry1", body: payload });
      })
    );
    qs("#btnSubmitDE1").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + id + "/data-entry1/submit",
          body: { comment: qs("#de1Comment").value.trim() || null },
        });
      })
    );
    qs("#btnApproveDE1").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + id + "/data-entry1/approve",
          body: { comment: qs("#de1ApproveComment").value.trim() || null },
        });
      })
    );
    qs("#btnReviseDE1").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const msg = qs("#de1RevisionMsg").value.trim();
        if (!msg) throw new Error("متن درخواست اصلاح الزامی است.");
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + id + "/data-entry1/revision-request",
          body: { message: msg },
        });
      })
    );

    // Data entry 2
    qs("#btnUpdateDE2").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const payload = parseJsonTextarea("#dataEntry2Json");
        await apiRequest({ method: "PUT", path: casesBasePath() + "/" + id + "/data-entry2", body: payload });
      })
    );
    qs("#btnSubmitDE2").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + id + "/data-entry2/submit",
          body: { comment: qs("#de2Comment").value.trim() || null },
        });
      })
    );
    qs("#btnApproveDE2").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + id + "/data-entry2/approve",
          body: { comment: qs("#de2ApproveComment").value.trim() || null },
        });
      })
    );
    qs("#btnReviseDE2").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const msg = qs("#de2RevisionMsg").value.trim();
        if (!msg) throw new Error("متن درخواست اصلاح الزامی است.");
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + id + "/data-entry2/revision-request",
          body: { message: msg },
        });
      })
    );

    // Valuations
    qs("#btnRecordValuation").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const payload = parseJsonTextarea("#valuationJson");
        await apiRequest({ method: "POST", path: casesBasePath() + "/" + id + "/valuations", body: payload });
      })
    );
    qs("#btnApproveValInitial").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + id + "/valuations/initial/approve",
          body: { comment: qs("#valInitialComment").value.trim() || null },
        });
      })
    );
    qs("#btnApproveValSecondary").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + id + "/valuations/secondary/approve",
          body: { comment: qs("#valSecondaryComment").value.trim() || null },
        });
      })
    );

    // Contracts
    qs("#btnUploadPreContract").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const s3Key = qs("#preContractS3Key").value.trim();
        if (!s3Key) throw new Error("s3Key الزامی است.");
        await apiRequest({ method: "POST", path: casesBasePath() + "/" + id + "/documents/confirm?s3Key=" + encodeURIComponent(s3Key), body: null, json: false });
      })
    );
    qs("#btnApprovePreContract").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + id + "/contracts/preliminary/approve",
          body: { comment: qs("#preContractApproveComment").value.trim() || null },
        });
      })
    );
    qs("#btnRequestPreContractRevision").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const msg = qs("#preContractRevisionMsg").value.trim();
        if (!msg) throw new Error("متن درخواست اصلاح الزامی است.");
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + id + "/contracts/preliminary/revision-request",
          body: { message: msg },
        });
      })
    );
    qs("#btnFinalizeDraft").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + id + "/contracts/finalize-draft",
          body: { comment: qs("#finalizeDraftComment").value.trim() || null },
        });
      })
    );
    qs("#btnConfirmSignature").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + id + "/contracts/confirm-signature",
          body: { comment: qs("#confirmSignatureComment").value.trim() || null },
        });
      })
    );
    qs("#btnUploadSignedContract").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const s3Key = qs("#signedContractS3Key").value.trim();
        if (!s3Key) throw new Error("s3Key الزامی است.");
        await apiRequest({ method: "POST", path: casesBasePath() + "/" + id + "/documents/confirm?s3Key=" + encodeURIComponent(s3Key), body: null, json: false });
      })
    );

    // Worksheet
    qs("#btnUpdateWorksheet").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const payload = parseJsonTextarea("#worksheetJson");
        await apiRequest({ method: "PUT", path: casesBasePath() + "/" + id + "/financial-worksheet", body: payload });
      })
    );
    qs("#btnSubmitWorksheet").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + id + "/financial-worksheet/submit",
          body: { comment: qs("#worksheetComment").value.trim() || null },
        });
      })
    );
    qs("#btnApproveWorksheet").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + id + "/financial-worksheet/approve",
          body: { comment: qs("#worksheetApproveComment").value.trim() || null },
        });
      })
    );
    qs("#btnRevisionWorksheet").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const msg = qs("#worksheetRevisionMsg").value.trim();
        if (!msg) throw new Error("متن درخواست اصلاح الزامی است.");
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + id + "/financial-worksheet/revision-request",
          body: { message: msg },
        });
      })
    );

    // Payments
    qs("#btnRecordPayment").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const payload = parseJsonTextarea("#paymentJson");
        await apiRequest({ method: "POST", path: casesBasePath() + "/" + id + "/payments", body: payload });
      })
    );
    qs("#btnConfirmPayment").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const pid = qs("#paymentId").value.trim();
        if (!pid) throw new Error("شناسه پرداخت الزامی است.");
        await apiRequest({ method: "POST", path: casesBasePath() + "/" + id + "/payments/" + encodeURIComponent(pid) + "/confirm", body: null, json: false });
      })
    );
    qs("#btnCancelPayment").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const pid = qs("#paymentId").value.trim();
        if (!pid) throw new Error("شناسه پرداخت الزامی است.");
        await apiRequest({ method: "POST", path: casesBasePath() + "/" + id + "/payments/" + encodeURIComponent(pid) + "/cancel", body: null, json: false });
      })
    );

    // Negative actions
    qs("#btnRejectCase").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const reason = qs("#rejectReason").value.trim();
        if (!reason) throw new Error("دلیل رد الزامی است.");
        await apiRequest({ method: "POST", path: casesBasePath() + "/" + id + "/reject", body: { reason } });
      })
    );
    qs("#btnCancelCase").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const reason = qs("#cancelReason").value.trim();
        if (!reason) throw new Error("دلیل لغو الزامی است.");
        await apiRequest({ method: "POST", path: casesBasePath() + "/" + id + "/cancel", body: { reason } });
      })
    );
    qs("#btnArchiveCase").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const reason = qs("#archiveReason").value.trim();
        if (!reason) throw new Error("دلیل بایگانی الزامی است.");
        await apiRequest({ method: "POST", path: casesBasePath() + "/" + id + "/archive", body: { reason } });
      })
    );
  }

  function wireDocuments() {
    let lastPresign = null;

    const fileInput = qs("#docFile");
    const mimeInput = qs("#docMime");
    fileInput.addEventListener("change", () => pickFileMime(fileInput, mimeInput));

    qs("#btnPresign").addEventListener("click", () =>
      withUiError(async () => {
        const caseId = requireCaseId();
        const file = pickFileMime(fileInput, mimeInput);
        if (!file) throw new Error("ابتدا یک فایل انتخاب کنید.");
        const docType = Number(qs("#docType").value);
        const payload = {
          documentType: docType,
          fileName: file.name,
          mimeType: mimeInput.value.trim() || file.type || "application/octet-stream",
          fileSize: file.size,
        };
        const res = await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + caseId + "/documents/presign",
          body: payload,
        });

        lastPresign = res.body && (res.body.s3Key ? res.body : res.body.Value || res.body.value || res.body.data || res.body.Data);
        if (!lastPresign || !lastPresign.Url && !lastPresign.url) {
          // Core service returns {s3Key,url,expiresAtUtc,version}
          lastPresign = res.body;
        }

        const s3Key = lastPresign.s3Key || lastPresign.S3Key || "";
        const url = lastPresign.url || lastPresign.Url || "";
        qs("#lastS3Key").textContent = s3Key;
        qs("#genericPutUrl").value = url;
        qs("#btnUploadToPresignUrl").disabled = !url;
        qs("#btnConfirmUpload").disabled = !s3Key;
      })
    );

    qs("#btnUploadToPresignUrl").addEventListener("click", () =>
      withUiError(async () => {
        const file = pickFileMime(fileInput, mimeInput);
        if (!file) throw new Error("ابتدا یک فایل انتخاب کنید.");
        if (!lastPresign) throw new Error("ابتدا آدرس بارگذاری را دریافت کنید.");
        const url = lastPresign.url || lastPresign.Url;
        const ct = mimeInput.value.trim() || file.type || "application/octet-stream";

        // Raw fetch: presigned URLs usually do NOT want Authorization header.
        const requestForInspector = {
          at: nowIso(),
          method: "PUT",
          url,
          headers: { "Content-Type": ct },
          body: { fileName: file.name, size: file.size, note: "binary omitted" },
        };
        inspector.lastRequest = requestForInspector;
        inspector.lastResponse = null;
        renderInspector();

        const start = performance.now();
        const res = await fetch(url, { method: "PUT", headers: { "Content-Type": ct }, body: file });
        const durationMs = Math.round(performance.now() - start);
        const text = await res.text().catch(() => "");
        inspector.lastResponse = {
          ok: res.ok,
          status: res.status,
          statusText: res.statusText,
          headers: {},
          body: text ? { raw: text } : { raw: "" },
        };
        inspector.lastStatus = String(res.status);
        inspector.lastTime = durationMs + "ms";
        inspector.log.unshift({
          time: nowIso(),
          method: "PUT",
          path: "(presigned url)",
          status: String(res.status),
          durationMs,
          request: requestForInspector,
          response: inspector.lastResponse,
        });
        inspector.log = inspector.log.slice(0, inspector.maxLog);
        renderInspector();

        if (!res.ok) throw new Error("بارگذاری با کد " + res.status + " ناموفق بود.");
      })
    );

    qs("#btnConfirmUpload").addEventListener("click", () =>
      withUiError(async () => {
        const caseId = requireCaseId();
        if (!lastPresign) throw new Error("ابتدا آدرس بارگذاری را دریافت کنید.");
        const s3Key = lastPresign.s3Key || lastPresign.S3Key;
        if (!s3Key) throw new Error("s3Key یافت نشد.");
        await apiRequest({
          method: "POST",
          path: casesBasePath() + "/" + caseId + "/documents/confirm?s3Key=" + encodeURIComponent(s3Key),
          body: null,
          json: false,
        });
      })
    );

    qs("#btnListDocs").addEventListener("click", () =>
      withUiError(async () => {
        const caseId = requireCaseId();
        await apiRequest({ method: "GET", path: casesBasePath() + "/" + caseId + "/documents" });
      })
    );

    qs("#btnPresignDownload").addEventListener("click", () =>
      withUiError(async () => {
        const caseId = requireCaseId();
        const docId = qs("#downloadDocumentId").value.trim();
        if (!docId) throw new Error("شناسه سند الزامی است.");
        const res = await apiRequest({
          method: "GET",
          path: casesBasePath() + "/" + caseId + "/documents/" + encodeURIComponent(docId) + "/download",
        });
        const body = res.body && (res.body.url ? res.body : res.body.Value || res.body.value || res.body.data || res.body.Data);
        const url = body && (body.url || body.Url);
        if (url) qs("#downloadLink").href = url;
      })
    );

    // Generic PUT tool
    const genericPutFile = qs("#genericPutFile");
    const genericPutCt = qs("#genericPutContentType");
    genericPutFile.addEventListener("change", () => pickFileMime(genericPutFile, genericPutCt));
    qs("#btnGenericPut").addEventListener("click", () =>
      withUiError(async () => {
        const url = qs("#genericPutUrl").value.trim();
        if (!url) throw new Error("آدرس الزامی است.");
        const file = pickFileMime(genericPutFile, genericPutCt);
        if (!file) throw new Error("فایل الزامی است.");
        const ct = genericPutCt.value.trim() || file.type || "application/octet-stream";
        const res = await fetch(url, { method: "PUT", headers: { "Content-Type": ct }, body: file });
        if (!res.ok) throw new Error("درخواست PUT با کد " + res.status + " ناموفق بود.");
      })
    );

    // Multipart POST tool
    qs("#btnMultipartPost").addEventListener("click", () =>
      withUiError(async () => {
        const path = qs("#multipartPath").value.trim();
        const field = qs("#multipartField").value.trim() || "file";
        const fileInput = qs("#multipartFile");
        const file = fileInput.files && fileInput.files[0];
        if (!path) throw new Error("مسیر الزامی است.");
        if (!file) throw new Error("فایل الزامی است.");

        const session = getActiveSession();
        const headers = {};
        if (session && session.accessToken) headers.Authorization = "Bearer " + session.accessToken;

        const fd = new FormData();
        fd.append(field, file, file.name);

        const url = makeUrl(path);
        const requestForInspector = {
          at: nowIso(),
          method: "POST",
          url,
          headers,
          body: { formData: true, field, fileName: file.name, size: file.size },
        };
        inspector.lastRequest = requestForInspector;
        inspector.lastResponse = null;
        renderInspector();

        const start = performance.now();
        const res = await fetch(url, { method: "POST", headers, body: fd });
        const durationMs = Math.round(performance.now() - start);
        const text = await res.text().catch(() => "");
        inspector.lastResponse = {
          ok: res.ok,
          status: res.status,
          statusText: res.statusText,
          headers: {},
          body: safeJsonParse(text, { raw: text }),
        };
        inspector.lastStatus = String(res.status);
        inspector.lastTime = durationMs + "ms";
        inspector.log.unshift({
          time: nowIso(),
          method: "POST",
          path,
          status: String(res.status),
          durationMs,
          request: requestForInspector,
          response: inspector.lastResponse,
        });
        inspector.log = inspector.log.slice(0, inspector.maxLog);
        renderInspector();
        if (!res.ok) throw new Error("درخواست چندبخشی با کد " + res.status + " ناموفق بود.");
      })
    );
  }

  function wireComments() {
    qs("#btnGetComments").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const includeInternal = qs("#includeInternal").value;
        await apiRequest({ method: "GET", path: casesBasePath() + "/" + id + "/comments?includeInternal=" + encodeURIComponent(includeInternal) });
      })
    );

    qs("#btnAddComment").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const payload = parseJsonTextarea("#commentJson");
        await apiRequest({ method: "POST", path: casesBasePath() + "/" + id + "/comments", body: payload });
      })
    );

    qs("#btnAddCommentAttachment").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const commentId = qs("#commentId").value.trim();
        const s3Key = qs("#commentAttachmentS3Key").value.trim();
        const fileName = qs("#commentAttachmentFileName").value.trim();
        if (!commentId || !s3Key || !fileName) throw new Error("commentId، s3Key و fileName الزامی هستند.");
        await apiRequest({
          method: "POST",
          path:
            casesBasePath() +
            "/" +
            id +
            "/comments/" +
            encodeURIComponent(commentId) +
            "/attachments?s3Key=" +
            encodeURIComponent(s3Key) +
            "&fileName=" +
            encodeURIComponent(fileName),
          body: null,
          json: false,
        });
      })
    );
  }

  function wireEvaluations() {
    qs("#btnUpsertEval").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        const payload = parseJsonTextarea("#evalJson");
        await apiRequest({ method: "POST", path: casesBasePath() + "/" + id + "/evaluations", body: payload });
      })
    );
    qs("#btnGetEvals").addEventListener("click", () =>
      withUiError(async () => {
        const id = requireCaseId();
        await apiRequest({ method: "GET", path: casesBasePath() + "/" + id + "/evaluations" });
      })
    );
  }

  function wireDebugTools() {
    qs("#btnManualSend").addEventListener("click", () =>
      withUiError(async () => {
        const method = qs("#manualMethod").value;
        const path = qs("#manualPath").value.trim();
        const auth = qs("#manualAuth").value;
        const headers = safeJsonParse(qs("#manualHeaders").value || "{}", {});
        const rawBody = (qs("#manualBody").value || "").trim();
        let body = null;
        let json = true;
        if (rawBody) {
          const maybe = safeJsonParse(rawBody, null);
          if (maybe == null) {
            body = rawBody;
            json = false;
          } else {
            body = maybe;
          }
        }
        await apiRequest({
          method,
          path,
          useAuth: auth !== "none",
          headers,
          body,
          json,
        });
      })
    );

    qs("#btnShowTokens").addEventListener("click", () =>
      withUiError(async () => {
        const s = getActiveSession();
        qs("#tokenViewer").textContent = s
          ? pretty({
              label: s.label,
              userId: s.userId,
              role: s.userRoleText,
              roleNumber: s.userRoleNumber,
              accessTokenExpiration: s.accessTokenExpiration,
              refreshTokenExpiration: s.refreshTokenExpiration,
              accessToken: s.accessToken,
              refreshToken: s.refreshToken,
            })
          : "No active session.";
      })
    );

    qs("#btnClearStorage").addEventListener("click", () =>
      withUiError(async () => {
        localStorage.removeItem(LS_SESSIONS);
        localStorage.removeItem(LS_ACTIVE);
        localStorage.removeItem(LS_STATE);
        renderSessionsList();
        renderTopbar();
        setCurrentCaseId("");
        qs("#tokenViewer").textContent = "";
      })
    );
  }

  function init() {
    fillSelects();
    wireTabs();
    wireConfigModal();
    wireAuth();
    wireUsers();
    wireCases();
    wireWorkflow();
    wireDocuments();
    wireComments();
    wireEvaluations();
    wireDebugTools();

    renderInspector();
    renderSessionsList();
    renderTopbar();

    const st = loadState();
    setCurrentCaseId(st.currentCaseId || "");

    // Make auth phone inputs a bit friendlier
    const active = getActiveSession();
    if (active && active.phone) {
      qs("#authPhone").value = active.phone;
      qs("#verifyPhone").value = active.phone;
    }

    setDefaultJsonTemplates();

    window.TestPanel = {
      apiRequest,
      casesBasePath,
      unwrapEnvelope,
      saveSessionFromLogin,
      getActiveSession,
      findSessionByPhone,
      setActiveSessionId,
      setCurrentCaseId,
    };

    if (typeof window.initKanban === "function") {
      window.initKanban(window.TestPanel);
    }

    if (typeof window.initPortal === "function") {
      window.initPortal(window.TestPanel);
    }

    if (typeof window.initWorkflowRunner === "function") {
      window.initWorkflowRunner(window.TestPanel);
    }
  }

  document.addEventListener("DOMContentLoaded", init);
})();

