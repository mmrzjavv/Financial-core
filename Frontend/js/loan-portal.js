/* global LoanWorkflowModel */
(function () {
  const model = window.LoanWorkflowModel;
  const state = { panel: null, caseId: "", caseData: null, documents: [], installments: [] };

  function lPath(suffix) {
    return state.panel.loanCasesBasePath() + suffix;
  }

  function unwrap(body) {
    return state.panel.unwrapEnvelope(body).payload;
  }

  function pick(obj, camel, pascal) {
    if (!obj) return undefined;
    if (obj[camel] !== undefined) return obj[camel];
    if (obj[pascal] !== undefined) return obj[pascal];
    return undefined;
  }

  function pickStatus(obj) {
    return Number(pick(obj, "currentStatus", "CurrentStatus") ?? 0);
  }

  function el(tag, cls, text) {
    const n = document.createElement(tag);
    if (cls) n.className = cls;
    if (text != null) n.textContent = text;
    return n;
  }

  function qs(sel) {
    return document.querySelector(sel);
  }

  function setError(msg) {
    const box = qs("#lPortalError");
    if (!msg) {
      box.classList.add("hidden");
      box.textContent = "";
      return;
    }
    box.textContent = msg;
    box.classList.remove("hidden");
  }

  function setInfo(msg) {
    const box = qs("#lPortalInfo");
    if (!msg) {
      box.classList.add("hidden");
      return;
    }
    box.textContent = msg;
    box.classList.remove("hidden");
  }

  function getSessionRole() {
    const session = state.panel.getActiveSession();
    if (!session) return "";
    return model.normalizeRole(session.userRoleText, session.userRoleNumber);
  }

  async function refreshCase() {
    if (!state.caseId) return;
    const res = await state.panel.apiRequest({ method: "GET", path: lPath("/" + state.caseId) });
    state.caseData = unwrap(res.body);
    state.panel.setLoanCaseId(state.caseId);
    renderCase();
    await loadDocuments();
    await loadInstallments();
  }

  async function loadDocuments() {
    if (!state.caseId) return;
    const res = await state.panel.apiRequest({ method: "GET", path: lPath("/" + state.caseId + "/documents") });
    state.documents = unwrap(res.body) || [];
  }

  async function loadInstallments() {
    if (!state.caseId) return;
    try {
      const res = await state.panel.apiRequest({ method: "GET", path: lPath("/" + state.caseId + "/installments") });
      state.installments = unwrap(res.body) || [];
    } catch (_) {
      state.installments = [];
    }
  }

  async function uploadDocument(docType, file) {
    const presign = await state.panel.apiRequest({
      method: "POST",
      path: lPath("/" + state.caseId + "/documents/presign"),
      body: { documentType: docType, fileName: file.name, mimeType: file.type || "application/octet-stream", fileSize: file.size },
    });
    const p = unwrap(presign.body);
    const uploadUrl = p.url || p.Url;
    const s3Key = p.s3Key || p.S3Key;
    await fetch(uploadUrl, { method: "PUT", body: file, headers: { "Content-Type": file.type || "application/octet-stream" } });
    await state.panel.apiRequest({ method: "POST", path: lPath("/" + state.caseId + "/documents/confirm?s3Key=" + encodeURIComponent(s3Key)) });
    await loadDocuments();
    setInfo("فایل بارگذاری شد.");
  }

  function renderCase() {
    const status = pickStatus(state.caseData);
    const step = model.stepForStatus(status);
    const role = getSessionRole();

    qs("#lPortalEmpty").classList.add("hidden");
    qs("#lPortalHeader").classList.remove("hidden");
    qs("#lCaseNumber").textContent = pick(state.caseData, "caseNumber", "CaseNumber") || "—";
    const lCaseIdEl = qs("#lCaseId");
    if (lCaseIdEl) lCaseIdEl.textContent = state.caseId;
    qs("#lCaseStatus").textContent = step.title + " (" + status + ")";
    qs("#lCaseRole").textContent = role || "—";

    const company = pick(state.caseData, "company", "Company");
    qs("#lCaseCompany").textContent = company ? pick(company, "name", "Name") || "—" : "—";

    const hint = qs("#lPortalActionHint");
    if (model.canActOnCase(status, role)) {
      hint.textContent = "اقدام شما لازم است";
      hint.classList.remove("hidden");
    } else {
      hint.classList.add("hidden");
    }

    renderStages(status, role);
  }

  function renderStages(status, role) {
    const host = qs("#lPortalStages");
    host.innerHTML = "";

    if ([1, 2, 4].includes(status)) {
      host.appendChild(renderApplicationForm(status));
      host.appendChild(renderDocumentGrid(model.DATA_ENTRY_DOCUMENTS));
      if (status === 1) {
        host.appendChild(actionBtn("شروع ورود اطلاعات", () => postEmpty("/application/begin")));
      }
      if ([2, 4].includes(status)) {
        host.appendChild(actionBtn("ارسال درخواست", () => postEmpty("/application/submit")));
      }
    }

    if (status === 3 && ["CreditExpert", "CreditManager", "Admin"].includes(role)) {
      host.appendChild(renderApprovalForm());
      host.appendChild(actionBtn("تأیید اعتبارات", () => postJson("/credit/approve", {})));
      host.appendChild(revisionBtn("/credit/revision-request"));
    }

    if (status === 5 && ["CEO", "Admin"].includes(role)) {
      host.appendChild(actionBtn("تأیید مدیرعامل (اول)", () => postJson("/ceo/initial/approve", {})));
      host.appendChild(revisionBtn("/ceo/initial/reject"));
    }

    if (status === 7 && ["LegalExpert", "LegalManager", "Admin"].includes(role)) {
      host.appendChild(renderDocumentGrid([{ type: 13, label: "قرارداد خام", required: true }]));
      host.appendChild(renderInstallmentEditor());
      host.appendChild(actionBtn("تکمیل قرارداد و اقساط", () => postEmpty("/legal/setup-complete")));
    }

    if ([8, 10, 12].includes(status) && role === "Applicant") {
      host.appendChild(renderDocumentGrid([
        { type: 15, label: "قرارداد امضاشده", required: true },
        { type: 16, label: "پیوست ۱", required: false },
        { type: 17, label: "پیوست ۲", required: false },
      ]));
      host.appendChild(actionBtn("ارسال قرارداد امضاشده", () => postEmpty("/signed-package/submit")));
    }

    if (status === 9 && ["LegalExpert", "LegalManager", "Admin"].includes(role)) {
      host.appendChild(actionBtn("تأیید حقوقی", () => postJson("/legal/approve", {})));
      host.appendChild(revisionBtn("/legal/revision-request"));
    }

    if (status === 11 && ["FinancialExpert", "FinancialManager", "Admin"].includes(role)) {
      host.appendChild(actionBtn("تأیید مالی", () => postJson("/financial/approve", {})));
      host.appendChild(revisionBtn("/financial/revision-request"));
    }

    if (status === 13 && ["LegalExpert", "LegalManager", "Admin"].includes(role)) {
      host.appendChild(renderDocumentGrid([{ type: 22, label: "قرارداد نهایی", required: true }]));
      host.appendChild(actionBtn("تأیید بارگذاری قرارداد نهایی", () => postEmpty("/legal/final-uploaded")));
    }

    if (status === 14 && ["CEO", "Admin"].includes(role)) {
      host.appendChild(actionBtn("تأیید نهایی مدیرعامل", () => postJson("/ceo/final/approve", {})));
      host.appendChild(revisionBtn("/ceo/final/reject"));
    }

    if ([15, 16].includes(status) && ["FinancialExpert", "FinancialManager", "Admin"].includes(role)) {
      host.appendChild(renderPaymentForm());
    }

    if ([16, 17].includes(status)) {
      host.appendChild(renderInstallmentDashboard());
    }
  }

  function renderApplicationForm(status) {
    const card = el("div", "card portal-card");
    card.appendChild(el("div", "card__title", "درخواست تسهیلات"));
    const app = pick(state.caseData, "application", "Application") || {};
    const fields = [
      ["lRequestedAmount", "مبلغ (ریال)", pick(app, "requestedAmount", "RequestedAmount")],
      ["lFacilitySubject", "موضوع تسهیلات", pick(app, "facilitySubject", "FacilitySubject")],
      ["lOfferedGuarantees", "تضامین و وثایق", pick(app, "offeredGuarantees", "OfferedGuarantees")],
    ];
    fields.forEach(([id, label, val]) => {
      const row = el("div", "formrow");
      row.appendChild(el("label", "", label));
      const input = document.createElement("input");
      input.id = id;
      input.value = val != null ? String(val) : "";
      row.appendChild(input);
      card.appendChild(row);
    });
    if ([1, 2, 4].includes(status)) {
      card.appendChild(
        actionBtn("ذخیره درخواست", async () => {
          await state.panel.apiRequest({
            method: "PUT",
            path: lPath("/" + state.caseId + "/application"),
            body: {
              requestedAmount: Number(qs("#lRequestedAmount").value) || null,
              facilitySubject: qs("#lFacilitySubject").value,
              offeredGuarantees: qs("#lOfferedGuarantees").value,
              applicantCategory: 1,
            },
          });
          await refreshCase();
        })
      );
    }
    return card;
  }

  function renderApprovalForm() {
    const card = el("div", "card portal-card");
    card.appendChild(el("div", "card__title", "فرم تصویب"));
    const detail = pick(state.caseData, "approvalDetail", "ApprovalDetail") || {};
    const row = el("div", "formrow");
    row.appendChild(el("label", "", "مبلغ تأییدشده"));
    const amount = document.createElement("input");
    amount.id = "lApprovedAmount";
    amount.value = pick(detail, "approvedAmount", "ApprovedAmount") || "";
    row.appendChild(amount);
    card.appendChild(row);
    card.appendChild(
      actionBtn("ذخیره فرم تصویب", async () => {
        await state.panel.apiRequest({
          method: "PUT",
          path: lPath("/" + state.caseId + "/approval-detail"),
          body: {
            approvedAmount: Number(amount.value) || null,
            facilityType: 3,
            contractSubject: pick(state.caseData?.application, "facilitySubject", "FacilitySubject"),
            repaymentMonths: 12,
            annualProfitRatePercent: 18,
            collateralDescription: "—",
            guarantorsDescription: "—",
          },
        });
        await refreshCase();
      })
    );
    return card;
  }

  function renderDocumentGrid(docs) {
    const card = el("div", "card portal-card");
    card.appendChild(el("div", "card__title", "مدارک"));
    docs.forEach((d) => {
      const row = el("div", "formrow");
      row.appendChild(el("label", "", d.label + (d.required ? " (ضروری)" : "")));
      const input = document.createElement("input");
      input.type = "file";
      input.addEventListener("change", () => {
        const file = input.files && input.files[0];
        if (file) void uploadDocument(d.type, file).catch((e) => setError(e.message));
      });
      row.appendChild(input);
      card.appendChild(row);
    });
    return card;
  }

  function renderInstallmentEditor() {
    const card = el("div", "card portal-card");
    card.appendChild(el("div", "card__title", "جدول اقساط (نمونه)"));
    card.appendChild(
      actionBtn("ثبت یک قساط نمونه", async () => {
        const today = new Date();
        const d = today.toISOString().slice(0, 10);
        await state.panel.apiRequest({
          method: "PUT",
          path: lPath("/" + state.caseId + "/installments"),
          body: {
            installments: [
              {
                rowNumber: 1,
                installmentDate: d,
                principalAmount: 1000000,
                profitAmount: 100000,
                totalAmount: 1100000,
                fundShareOfPrincipal: 1000000,
                fundShareOfProfit: 100000,
                fundShareOfTotal: 1100000,
                isGracePeriod: false,
              },
            ],
          },
        });
        await refreshCase();
      })
    );
    return card;
  }

  function renderPaymentForm() {
    const card = el("div", "card portal-card");
    card.appendChild(el("div", "card__title", "ثبت پرداخت"));
    card.appendChild(
      actionBtn("ثبت پرداخت نمونه", async () => {
        const today = new Date().toISOString().slice(0, 10);
        await state.panel.apiRequest({
          method: "POST",
          path: lPath("/" + state.caseId + "/payments"),
          body: { amount: 5000000, paymentDate: today, transactionNumber: "TX-" + Date.now(), stageNumber: 1 },
        });
        await refreshCase();
      })
    );
    return card;
  }

  function renderInstallmentDashboard() {
    const card = el("div", "card portal-card");
    card.appendChild(el("div", "card__title", "داشبورد اقساط متقاضی"));
    if (!state.installments.length) {
      card.appendChild(el("div", "muted", "اقساطی ثبت نشده است."));
      return card;
    }
    const table = document.createElement("table");
    table.className = "data-table";
    table.innerHTML = "<thead><tr><th>ردیف</th><th>تاریخ</th><th>مبلغ</th><th>وضعیت</th></tr></thead>";
    const tbody = document.createElement("tbody");
    state.installments.forEach((i) => {
      const tr = document.createElement("tr");
      const paid = pick(i, "isPaid", "IsPaid");
      tr.innerHTML =
        "<td>" +
        pick(i, "rowNumber", "RowNumber") +
        "</td><td>" +
        pick(i, "installmentDate", "InstallmentDate") +
        "</td><td>" +
        pick(i, "totalAmount", "TotalAmount") +
        "</td><td>" +
        (paid ? "پرداخت‌شده" : "پرداخت‌نشده") +
        "</td>";
      tbody.appendChild(tr);
    });
    table.appendChild(tbody);
    card.appendChild(table);
    return card;
  }

  function actionBtn(label, handler) {
    const btn = el("button", "btn btn--primary", label);
    btn.type = "button";
    btn.addEventListener("click", () => {
      void handler().catch((e) => setError(e.message || String(e)));
    });
    const wrap = el("div", "row");
    wrap.appendChild(btn);
    return wrap;
  }

  function revisionBtn(path) {
    const wrap = el("div", "row");
    const input = document.createElement("input");
    input.placeholder = "پیام اصلاح";
    const btn = el("button", "btn btn--warn", "درخواست اصلاح");
    btn.type = "button";
    btn.addEventListener("click", () => {
      void postJson(path, { message: input.value || "اصلاح لازم است" }).catch((e) => setError(e.message));
    });
    wrap.appendChild(input);
    wrap.appendChild(btn);
    return wrap;
  }

  async function postEmpty(path) {
    setError("");
    await state.panel.apiRequest({ method: "POST", path: lPath("/" + state.caseId + path) });
    await refreshCase();
    setInfo("عملیات انجام شد.");
  }

  async function postJson(path, body) {
    setError("");
    await state.panel.apiRequest({ method: "POST", path: lPath("/" + state.caseId + path), body });
    await refreshCase();
    setInfo("عملیات انجام شد.");
  }

  function wireCreateCase() {
    qs("#lApplicantType").addEventListener("change", () => {
      qs("#lCompanyRow").style.display = qs("#lApplicantType").value === "2" ? "" : "none";
    });
    qs("#lLoadCompanies").addEventListener("click", () => {
      void (async () => {
        const res = await state.panel.apiRequest({ method: "GET", path: "/api/v1/identity/companies/mine" });
        const list = unwrap(res.body) || [];
        const sel = qs("#lCompanyId");
        sel.innerHTML = "";
        list.forEach((c) => {
          const opt = document.createElement("option");
          opt.value = pick(c, "id", "Id");
          opt.textContent = pick(c, "name", "Name");
          sel.appendChild(opt);
        });
      })().catch((e) => setError(e.message));
    });
    qs("#lCreateCase").addEventListener("click", () => {
      void (async () => {
        setError("");
        const applicantType = Number(qs("#lApplicantType").value);
        const companyId = qs("#lCompanyId").value || null;
        const res = await state.panel.apiRequest({
          method: "POST",
          path: lPath(""),
          body: { applicantType, companyId: companyId || null },
        });
        const data = unwrap(res.body);
        state.caseId = pick(data, "id", "Id");
        state.panel.setLoanCaseId(state.caseId);
        await refreshCase();
        setInfo("پرونده تسهیلات ایجاد شد.");
      })().catch((e) => setError(e.message));
    });
    qs("#lLoadCase").addEventListener("click", () => {
      state.caseId = qs("#lCaseIdInput").value.trim();
      if (!state.caseId) return;
      void refreshCase().catch((e) => setError(e.message));
    });
    qs("#lPortalRefreshCase").addEventListener("click", () => {
      void refreshCase().catch((e) => setError(e.message));
    });
  }

  window.initLoanPortal = function initLoanPortal(panel) {
    state.panel = panel;
    if (qs("#lCreateCase")) wireCreateCase();
    const id = panel.getLoanCaseId && panel.getLoanCaseId();
    if (id) {
      state.caseId = id;
      void refreshCase();
    }
    document.addEventListener("testpanel:case-changed", (ev) => {
      if (ev.detail?.module !== "loan") return;
      state.caseId = ev.detail?.caseId || panel.getLoanCaseId() || "";
      if (state.caseId) void refreshCase().catch((e) => setError(e.message));
    });
  };
})();
