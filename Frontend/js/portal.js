/* global WorkflowModel */
(function () {
  const model = window.WorkflowModel;
  const state = {
    panel: null,
    caseId: "",
    caseData: null,
    history: [],
    documents: [],
    comments: [],
    selectedUnit: "all",
    busy: false,
  };

  const qs = (sel, root) => (root || document).querySelector(sel);

  function setPortalError(message) {
    const box = qs("#portalError");
    if (!box) return;
    if (!message) {
      box.classList.add("hidden");
      box.textContent = "";
      return;
    }
    box.classList.remove("hidden");
    box.textContent = message;
  }

  function setPortalInfo(message) {
    const box = qs("#portalInfo");
    if (!box) return;
    if (!message) {
      box.classList.add("hidden");
      box.textContent = "";
      return;
    }
    box.classList.remove("hidden");
    box.textContent = message;
  }

  function pickId(obj) {
    if (!obj || typeof obj !== "object") return "";
    return obj.id || obj.Id || "";
  }

  function pickStatus(obj) {
    if (!obj || typeof obj !== "object") return 0;
    return Number(obj.currentStatus ?? obj.CurrentStatus ?? 0);
  }

  function pickPhase(obj) {
    if (!obj || typeof obj !== "object") return 0;
    return Number(obj.currentPhase ?? obj.CurrentPhase ?? 0);
  }

  function pickCaseNumber(obj) {
    if (!obj || typeof obj !== "object") return "";
    return obj.caseNumber || obj.CaseNumber || "";
  }

  function unwrap(body) {
    return state.panel.unwrapEnvelope(body).payload;
  }

  function casesPath(suffix) {
    return state.panel.casesBasePath() + suffix;
  }

  function getSessionRole() {
    const session = state.panel.getActiveSession();
    if (!session) return "";
    return model.normalizeRole(session.userRoleText, session.userRoleNumber);
  }

  function isInternalSession() {
    return model.isInternalRole(getSessionRole());
  }

  function readCaseId() {
    const input = qs("#currentCaseId");
    const fromInput = input ? input.value.trim() : "";
    const fromState = (() => {
      try {
        return JSON.parse(localStorage.getItem("workflow_test_panel.state.v1") || "{}").currentCaseId || "";
      } catch {
        return "";
      }
    })();
    return fromInput || fromState || state.caseId;
  }

  function readValue(id) {
    const node = qs("#" + id);
    return node ? node.value.trim() : "";
  }

  function readNumber(id) {
    const value = Number(readValue(id));
    return Number.isFinite(value) ? value : 0;
  }

  function readChecked(id) {
    const node = qs("#" + id);
    return !!(node && node.checked);
  }

  async function uploadDocument(caseId, documentType, file) {
    const mimeType = file.type || "application/octet-stream";
    const presign = await state.panel.apiRequest({
      method: "POST",
      path: casesPath("/" + caseId + "/documents/presign"),
      body: {
        documentType,
        fileName: file.name,
        mimeType,
        fileSize: file.size,
      },
    });
    const payload = unwrap(presign.body);
    const url = payload.url || payload.Url;
    const s3Key = payload.s3Key || payload.S3Key;
    if (!url || !s3Key) throw new Error("پاسخ presign ناقص است.");

    const putRes = await fetch(url, {
      method: "PUT",
      headers: { "Content-Type": mimeType },
      body: file,
    });
    if (!putRes.ok) throw new Error("بارگذاری فایل با کد " + putRes.status + " ناموفق بود.");

    await state.panel.apiRequest({
      method: "POST",
      path: casesPath("/" + caseId + "/documents/confirm?s3Key=" + encodeURIComponent(s3Key)),
      body: null,
      json: false,
    });
    return s3Key;
  }

  async function uploadContract(caseId, kind, file) {
    const docType = kind === "signed" ? 9 : 7;
    const s3Key = await uploadDocument(caseId, docType, file);
    const route = kind === "signed" ? "/contracts/signed/upload" : "/contracts/preliminary/upload";
    await state.panel.apiRequest({
      method: "POST",
      path: casesPath("/" + caseId + route + "?s3Key=" + encodeURIComponent(s3Key)),
      body: null,
      json: false,
    });
    return s3Key;
  }

  async function postComment(caseId, phase, message, isInternal) {
    if (!message) throw new Error("متن نظر الزامی است.");
    await state.panel.apiRequest({
      method: "POST",
      path: casesPath("/" + caseId + "/comments"),
      body: {
        phase,
        message,
        isInternal: !!isInternal,
        parentId: null,
      },
    });
  }

  async function refreshCase() {
    const caseId = readCaseId();
    state.caseId = caseId;
    if (!caseId) {
      state.caseData = null;
      state.history = [];
      state.documents = [];
      state.comments = [];
      render();
      return;
    }

    const session = state.panel.getActiveSession();
    if (!session) throw new Error("ابتدا وارد سامانه شوید.");

    const caseRes = await state.panel.apiRequest({ method: "GET", path: casesPath("/" + caseId) });
    state.caseData = unwrap(caseRes.body);

    const historyRes = await state.panel.apiRequest({ method: "GET", path: casesPath("/" + caseId + "/history") });
    state.history = unwrap(historyRes.body) || [];

    try {
      const docsRes = await state.panel.apiRequest({ method: "GET", path: casesPath("/" + caseId + "/documents") });
      state.documents = unwrap(docsRes.body) || [];
    } catch {
      state.documents = [];
    }

    try {
      const includeInternal = isInternalSession() ? "true" : "false";
      const commentsRes = await state.panel.apiRequest({
        method: "GET",
        path: casesPath("/" + caseId + "/comments?includeInternal=" + includeInternal),
      });
      const payload = unwrap(commentsRes.body);
      state.comments = Array.isArray(payload) ? payload : payload ? [payload] : [];
    } catch {
      state.comments = [];
    }

    state.panel.setCurrentCaseId(caseId);
    render();
  }

  function el(tag, className, text) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text != null) node.textContent = text;
    return node;
  }

  function field(label, id, type, value) {
    const row = el("div", "formrow");
    row.appendChild(el("label", "", label));
    const input = document.createElement(type === "textarea" ? "textarea" : "input");
    input.id = id;
    if (type === "textarea") input.className = "portal-textarea";
    else input.type = type || "text";
    if (value != null && value !== "") input.value = value;
    row.appendChild(input);
    return row;
  }

  function actionRow(buttons) {
    const row = el("div", "row");
    buttons.forEach((btn) => row.appendChild(btn));
    return row;
  }

  function createButton(label, className, action, extra) {
    const btn = el("button", "btn " + (className || ""), label);
    btn.type = "button";
    btn.dataset.action = action;
    if (extra) {
      Object.keys(extra).forEach((key) => {
        btn.dataset[key] = extra[key];
      });
    }
    return btn;
  }

  let uploadFieldCounter = 0;

  function nextUploadFieldId(prefix) {
    uploadFieldCounter += 1;
    return (prefix || "upload") + "-" + uploadFieldCounter;
  }

  function appendFileUploadRow(parent, options) {
    const row = el("div", "portal-upload-row");
    const inputId = options.id || nextUploadFieldId(options.idPrefix || "file");

    if (options.title || options.hint) {
      const meta = el("div", "portal-upload-row__meta");
      if (options.title) meta.appendChild(el("div", "portal-upload-row__title", options.title));
      if (options.hint) meta.appendChild(el("div", "muted", options.hint));
      row.appendChild(meta);
    }

    const control = el("div", "portal-upload-row__control");
    const input = document.createElement("input");
    input.type = "file";
    input.id = inputId;
    input.className = "portal-file-input";
    input.accept = options.accept || ".pdf,.png,.jpg,.jpeg,application/pdf,image/png,image/jpeg";

    if (options.uploadType != null) input.dataset.uploadType = String(options.uploadType);
    if (options.contractKind) input.dataset.contractKind = options.contractKind;
    if (options.contractCommentField) input.dataset.contractCommentField = options.contractCommentField;
    if (options.contractCommentPhase) input.dataset.contractCommentPhase = String(options.contractCommentPhase);
    if (options.paymentReceipt) input.dataset.paymentReceipt = "true";

    const picker = document.createElement("label");
    picker.className = "portal-file-btn";
    picker.htmlFor = inputId;
    picker.textContent = options.buttonText || "انتخاب و بارگذاری فایل";

    const status = el("span", "portal-upload-row__status muted");
    input.addEventListener("change", () => {
      const file = input.files && input.files[0];
      status.textContent = file ? "فایل انتخاب‌شده: " + file.name : "";
    });

    control.appendChild(input);
    control.appendChild(picker);
    control.appendChild(status);
    row.appendChild(control);
    parent.appendChild(row);
    return input;
  }

  function renderStepper() {
    const root = qs("#portalStepper");
    if (!root) return;
    root.innerHTML = "";
    const current = pickStatus(state.caseData);
    const track = el("div", "portal-stepper__track");

    model.STEPS.filter((step) => step.status <= 16).forEach((step) => {
      const item = el("div", "portal-stepper__item");
      if (step.status < current) item.classList.add("is-done");
      if (step.status === current) item.classList.add("is-current");
      if (step.status > current) item.classList.add("is-upcoming");

      item.appendChild(el("div", "portal-stepper__index", String(step.status)));
      item.appendChild(el("div", "portal-stepper__title", step.title));
      item.appendChild(el("div", "portal-stepper__unit", model.getUnit(step.unit)?.label || step.unit));
      track.appendChild(item);
    });

    root.appendChild(track);
  }

  function renderUnitTabs() {
    const root = qs("#portalUnitTabs");
    if (!root) return;
    root.innerHTML = "";
    const role = getSessionRole();

    model.UNITS.forEach((unit) => {
      const btn = el("button", "portal-unit-tab", unit.label);
      btn.type = "button";
      if (state.selectedUnit === unit.id) btn.classList.add("is-active");
      if (unit.id !== "all" && role && !model.roleMatchesUnit(role, unit.id)) btn.classList.add("is-muted");
      btn.dataset.unit = unit.id;
      root.appendChild(btn);
    });
  }

  function commentsForPhase(phase) {
    return state.comments.filter((comment) => Number(comment.phase ?? comment.Phase) === Number(phase));
  }

  function renderCommentsBlock(card, phase, prefix, options) {
    const block = el("div", "portal-thread");
    block.appendChild(el("div", "portal-thread__title", options.title || "گفتگو و نظرات"));

    const list = el("div", "portal-thread__list");
    const items = commentsForPhase(phase);
    if (!items.length) {
      list.appendChild(el("div", "muted", "هنوز نظری ثبت نشده است."));
    } else {
      items.forEach((comment) => {
        const row = el("div", "portal-thread__item");
        const meta = el("div", "portal-thread__meta");
        const role = comment.senderRole || comment.SenderRole || "";
        const internal = comment.isInternal || comment.IsInternal;
        const revision = comment.isRevisionRequest || comment.IsRevisionRequest;
        meta.textContent = [role, internal ? "داخلی" : "عمومی", revision ? "درخواست اصلاح" : ""].filter(Boolean).join(" · ");
        const body = el("div", "portal-thread__message", comment.message || comment.Message || "");
        row.appendChild(meta);
        row.appendChild(body);
        list.appendChild(row);
      });
    }
    block.appendChild(list);

    if (options.readOnly) {
      card.appendChild(block);
      return;
    }

    block.appendChild(field("متن نظر", prefix + "Comment", "textarea"));
    if (options.allowInternal) {
      const row = el("div", "formrow");
      const checkbox = document.createElement("input");
      checkbox.type = "checkbox";
      checkbox.id = prefix + "Internal";
      const label = el("label", "", "نظر داخلی (فقط واحدها)");
      label.htmlFor = prefix + "Internal";
      row.appendChild(checkbox);
      row.appendChild(label);
      block.appendChild(row);
    }

    block.appendChild(
      actionRow([
        createButton("ثبت نظر", "btn--primary", "post-comment", {
          commentPhase: String(phase),
          commentPrefix: prefix,
        }),
      ])
    );
    card.appendChild(block);
  }

  function renderUploads(card, prefix, title) {
    const wrap = el("div", "card portal-card portal-card--nested");
    wrap.appendChild(el("div", "card__title", title || "بارگذاری مدارک"));
    wrap.appendChild(el("div", "muted", "پس از انتخاب فایل، بارگذاری به‌صورت خودکار انجام می‌شود."));
    model.APPLICANT_DOCUMENTS.forEach((doc) => {
      appendFileUploadRow(wrap, {
        id: prefix + "-doc-" + doc.type,
        title: doc.label,
        hint: doc.hint,
        uploadType: doc.type,
        idPrefix: prefix,
      });
    });
    card.appendChild(wrap);
  }

  function renderDocumentsList(host) {
    const card = el("div", "card portal-card");
    card.appendChild(el("div", "card__title", "اسناد بارگذاری‌شده"));
    if (!state.documents.length) {
      card.appendChild(el("div", "muted", "هنوز سندی ثبت نشده است."));
    } else {
      const list = el("div", "portal-doc-list");
      state.documents.forEach((doc) => {
        const item = el("div", "portal-doc-list__item");
        const name = doc.fileName || doc.FileName || doc.documentType || doc.DocumentType || "سند";
        item.textContent = name;
        list.appendChild(item);
      });
      card.appendChild(list);
    }
    host.appendChild(card);
  }

  function renderHistory(host) {
    const card = el("div", "card portal-card");
    card.appendChild(el("div", "card__title", "تاریخچه اخیر"));
    if (!state.history.length) {
      card.appendChild(el("div", "muted", "تاریخچه‌ای ثبت نشده است."));
    } else {
      const list = el("div", "portal-history");
      state.history.slice(0, 12).forEach((item) => {
        const row = el("div", "portal-history__item");
        const status = item.toStatus || item.ToStatus || item.status || item.Status || "";
        const action = item.action || item.Action || "";
        row.textContent = [action, status].filter(Boolean).join(" → ");
        list.appendChild(row);
      });
      card.appendChild(list);
    }
    host.appendChild(card);
  }

  function renderDataEntryStage(card, prefix, phase, isReview) {
    if (!isReview) {
      if (prefix === "de1") {
        card.appendChild(field("عنوان استارتاپ", "de1Title", "text"));
        card.appendChild(field("شرح کسب‌وکار", "de1Description", "textarea"));
        card.appendChild(field("مبلغ درخواستی", "de1Amount", "number"));
        card.appendChild(field("تعداد تیم", "de1Team", "number"));
        card.appendChild(field("وب‌سایت", "de1Website", "text"));
        card.appendChild(field("کشور", "de1Country", "text", "IR"));
        card.appendChild(field("شهر", "de1City", "text"));
        card.appendChild(actionRow([
          createButton("ذخیره فرم اولیه", "btn--primary", "save-de1"),
          createButton("ارسال برای بررسی", "", "submit-de1"),
        ]));
        card.appendChild(field("توضیح همراه ارسال", "de1SubmitComment", "text"));
        renderUploads(card, "de1", "مدارک متقاضی (فرم اولیه)");
      } else {
        card.appendChild(field("تحلیل بازار", "de2Market", "textarea"));
        card.appendChild(field("مدل درآمد", "de2Revenue", "textarea"));
        card.appendChild(field("مزیت رقابتی", "de2Advantage", "textarea"));
        card.appendChild(field("پیش‌بینی مالی", "de2Projection", "textarea"));
        card.appendChild(actionRow([
          createButton("ذخیره فرم تکمیلی", "btn--primary", "save-de2"),
          createButton("ارسال برای بررسی", "", "submit-de2"),
        ]));
        card.appendChild(field("توضیح همراه ارسال", "de2SubmitComment", "text"));
        renderUploads(card, "de2", "مدارک متقاضی (فرم تکمیلی)");
      }
      renderCommentsBlock(card, phase, prefix, { allowInternal: false, title: "گفتگوی متقاضی و کارشناس" });
      return;
    }

    renderUploads(card, prefix + "rev", "مدارک تکمیلی کارشناس سرمایه‌گذاری");
    card.appendChild(actionRow([
      createButton("تأیید", "btn--primary", prefix === "de1" ? "approve-de1" : "approve-de2"),
      createButton("درخواست اصلاح", "btn--warn", prefix === "de1" ? "revise-de1" : "revise-de2"),
    ]));
    card.appendChild(field("پیام اصلاح (برگشت به متقاضی)", prefix + "Revision", "textarea"));
    card.appendChild(field("توضیح تأیید", prefix + "ApproveComment", "text"));
    renderCommentsBlock(card, phase, prefix + "rev", {
      allowInternal: true,
      title: "گفتگوی بررسی کارشناس",
    });
  }

  function renderStageForStatus(host, status, activeOnly) {
    const current = pickStatus(state.caseData);
    const isCurrent = status === current;
    if (activeOnly && !isCurrent) return;

    const step = model.getStep(status);
    if (!step) return;
    if (state.selectedUnit !== "all" && step.unit !== state.selectedUnit) return;

    const card = el("div", "card portal-card portal-stage");
    if (isCurrent) card.classList.add("is-current");
    card.dataset.stageStatus = String(status);
    card.appendChild(el("div", "card__title", "مرحله " + status + " — " + step.title));
    card.appendChild(el("div", "muted", model.getUnit(step.unit)?.label || ""));

    const role = getSessionRole();
    const phase = model.phaseForStatus(status);

    if (!isCurrent) {
      card.appendChild(el("div", "portal-stage__hint", status < current ? "این مرحله گذرانده شده است." : "این مرحله هنوز نرسیده است."));
      host.appendChild(card);
      return;
    }

    if ((status === 6 || status === 7) && role === "Applicant") {
      card.appendChild(el("div", "portal-stage__hint", "پرونده در انتظار ارزش‌گذاری توسط رئیس سرمایه‌گذاری است. از سمت شما اقدامی لازم نیست."));
      renderCommentsBlock(card, phase, "valView", { readOnly: true, title: "وضعیت ارزش‌گذاری" });
      host.appendChild(card);
      return;
    }

    if (!model.canActOnCase(role, step.unit)) {
      card.appendChild(el("div", "portal-stage__hint", "با نقش فعلی، اقدام این مرحله برای شما فعال نیست. جلسه نقش مناسب را انتخاب کنید."));
      renderCommentsBlock(card, phase, "view" + status, { readOnly: true, title: "نمای مشاهده" });
      host.appendChild(card);
      return;
    }

    if (status === 1 || status === 2) {
      if (status === 1) {
        card.appendChild(
          el(
            "div",
            "portal-stage__hint",
            "پرونده در پیش‌نویس است. فرم را ذخیره کنید؛ با اولین ارسال به مرحله فرم اولیه می‌روید و پس از تکمیل، دوباره ارسال برای بررسی کارشناس انجام دهید."
          )
        );
      }
      renderDataEntryStage(card, "de1", 1, false);
    } else if (status === 3) {
      renderDataEntryStage(card, "de1", 1, true);
    } else if (status === 4) {
      renderDataEntryStage(card, "de2", 1, false);
    } else if (status === 5) {
      renderDataEntryStage(card, "de2", 1, true);
    } else if (status === 6 || status === 7) {
      card.appendChild(field("مبلغ ارزش‌گذاری", "valuationAmount", "number"));
      card.appendChild(field("نوع (1 اولیه / 2 ثانویه)", "valuationType", "number", String(status === 6 ? 1 : 2)));
      card.appendChild(field("یادداشت", "valuationNotes", "textarea"));
      card.appendChild(actionRow([createButton("ثبت ارزش‌گذاری", "btn--primary", "record-valuation")]));
      card.appendChild(
        actionRow([
          createButton("تأیید ارزش‌گذاری اولیه", "btn--primary", "approve-val-initial"),
          createButton("تأیید ارزش‌گذاری ثانویه", "btn--primary", "approve-val-secondary"),
        ])
      );
      renderCommentsBlock(card, phase, "val", { allowInternal: true, title: "یادداشت‌های ارزش‌گذاری" });
    } else if (status === 8) {
      card.appendChild(field("توضیح همراه بارگذاری پیش‌قرارداد", "legalPreContractComment", "textarea"));
      appendFileUploadRow(card, {
        idPrefix: "legal-pre",
        title: "فایل پیش‌قرارداد",
        hint: "پس از انتخاب فایل، پیش‌قرارداد بارگذاری و پرونده به مرحله بعد می‌رود.",
        contractKind: "preliminary",
        contractCommentField: "legalPreContractComment",
        contractCommentPhase: "3",
      });
      renderCommentsBlock(card, phase, "legalPre", { allowInternal: true, title: "گفتگوی حقوقی" });
    } else if (status === 9) {
      card.appendChild(actionRow([
        createButton("تأیید پیش‌قرارداد", "btn--primary", "approve-pre-contract"),
        createButton("درخواست اصلاح", "btn--warn", "revise-pre-contract"),
      ]));
      card.appendChild(field("پیام اصلاح برای واحد حقوقی", "preContractRevision", "textarea"));
      card.appendChild(field("توضیح تأیید", "preContractApproveComment", "text"));
      renderCommentsBlock(card, phase, "legalUser", { allowInternal: false, title: "بازبینی متقاضی" });
    } else if (status === 10) {
      card.appendChild(field("توضیح تدوین قرارداد", "finalizeContractComment", "textarea"));
      appendFileUploadRow(card, {
        idPrefix: "legal-draft",
        title: "نسخه پیش‌نویس قرارداد (اختیاری)",
        hint: "فایل پیش‌نویس را بارگذاری کنید؛ سپس «نهایی‌سازی پیش‌نویس» را بزنید.",
        uploadType: 8,
      });
      card.appendChild(actionRow([createButton("نهایی‌سازی پیش‌نویس", "btn--primary", "finalize-contract")]));
      renderCommentsBlock(card, phase, "legalDraft", { allowInternal: true, title: "گفتگوی تدوین قرارداد" });
    } else if (status === 11) {
      card.appendChild(field("توضیح تأیید امضا", "confirmSignatureComment", "text"));
      card.appendChild(actionRow([createButton("تأیید امضا", "btn--primary", "confirm-signature")]));
      renderCommentsBlock(card, phase, "legalSign", { allowInternal: true, title: "گفتگوی امضا" });
    } else if (status === 12) {
      card.appendChild(field("توضیح بارگذاری قرارداد امضاشده", "signedContractComment", "textarea"));
      appendFileUploadRow(card, {
        idPrefix: "legal-signed",
        title: "قرارداد امضاشده",
        hint: "پس از انتخاب فایل، قرارداد امضاشده بارگذاری و پرونده به مرحله بعد می‌رود.",
        contractKind: "signed",
        contractCommentField: "signedContractComment",
        contractCommentPhase: "3",
      });
      renderCommentsBlock(card, phase, "legalSigned", { allowInternal: true, title: "گفتگوی بارگذاری نهایی" });
    } else if (status === 13) {
      card.appendChild(field("نام بانک", "wsBank", "text"));
      card.appendChild(field("شبا", "wsIban", "text"));
      card.appendChild(field("مبلغ مصوب", "wsApproved", "number"));
      card.appendChild(field("برنامه پرداخت", "wsSchedule", "textarea"));
      card.appendChild(field("یادداشت", "wsNotes", "textarea"));
      card.appendChild(actionRow([
        createButton("ذخیره کاربرگ", "btn--primary", "save-worksheet"),
        createButton("ارسال برای واحد مالی", "", "submit-worksheet"),
      ]));
      card.appendChild(field("توضیح ارسال کاربرگ", "wsSubmitComment", "text"));
      renderCommentsBlock(card, phase, "wsInv", { allowInternal: true, title: "گفتگوی سرمایه‌گذاری و مالی" });
    } else if (status === 14) {
      card.appendChild(actionRow([
        createButton("تأیید کاربرگ", "btn--primary", "approve-worksheet"),
        createButton("درخواست اصلاح", "btn--warn", "revise-worksheet"),
      ]));
      card.appendChild(field("پیام اصلاح برای سرمایه‌گذاری", "wsRevision", "textarea"));
      card.appendChild(field("توضیح تأیید", "wsApproveComment", "text"));
      renderCommentsBlock(card, phase, "wsFin", { allowInternal: true, title: "بررسی واحد مالی" });
    } else if (status === 15) {
      card.appendChild(field("مبلغ", "payAmount", "number"));
      card.appendChild(field("تاریخ پرداخت", "payDate", "date"));
      card.appendChild(field("شماره تراکنش", "payTxn", "text"));
      card.appendChild(field("روش (1-4)", "payMethod", "number", "1"));
      card.appendChild(field("وضعیت (1-4)", "payStatus", "number", "1"));
      appendFileUploadRow(card, {
        idPrefix: "payment-receipt",
        title: "رسید پرداخت",
        hint: "رسید را انتخاب کنید؛ با «ثبت پرداخت» بارگذاری و ثبت می‌شود.",
        paymentReceipt: true,
        buttonText: "انتخاب رسید",
      });
      card.appendChild(actionRow([createButton("ثبت پرداخت", "btn--primary", "record-payment")]));
      card.appendChild(field("شناسه پرداخت برای تأیید/لغو", "payId", "text"));
      card.appendChild(
        actionRow([
          createButton("تأیید پرداخت", "btn--primary", "confirm-payment"),
          createButton("لغو پرداخت", "btn--warn", "cancel-payment"),
        ])
      );
      renderCommentsBlock(card, phase, "pay", { allowInternal: true, title: "گفتگوی پرداخت" });
    }

    host.appendChild(card);
  }

  function renderStageHost() {
    const host = qs("#portalStageHost");
    if (!host) return;
    host.innerHTML = "";

    if (!state.caseData) {
      host.appendChild(el("div", "portal-empty", "برای شروع، یک پرونده انتخاب کنید یا از تب پرونده‌ها پرونده جدید بسازید."));
      return;
    }

    const current = pickStatus(state.caseData);
    if (state.selectedUnit === "all") {
      model.STEPS.filter((step) => step.status <= 15).forEach((step) => renderStageForStatus(host, step.status, false));
    } else {
      renderStageForStatus(host, current, true);
    }

    renderDocumentsList(host);
    renderHistory(host);
  }

  function renderSummary() {
    const empty = qs("#portalCaseEmpty");
    const header = qs("#portalCaseHeader");
    if (!empty || !header) return;

    if (!state.caseData) {
      empty.classList.remove("hidden");
      header.classList.add("hidden");
      return;
    }

    empty.classList.add("hidden");
    header.classList.remove("hidden");
    qs("#portalCaseNumber").textContent = pickCaseNumber(state.caseData) || "—";
    qs("#portalCaseId").textContent = pickId(state.caseData) || state.caseId;
    const step = model.getStep(pickStatus(state.caseData));
    qs("#portalCaseStatus").textContent = step ? step.title : String(pickStatus(state.caseData));
    qs("#portalCasePhase").textContent = model.PHASES[pickPhase(state.caseData)] || "—";
    qs("#portalCaseRole").textContent = getSessionRole() || "بدون نقش";
  }

  function render() {
    renderSummary();
    renderStepper();
    renderUnitTabs();
    renderStageHost();
  }

  async function withBusy(fn) {
    if (state.busy) return;
    state.busy = true;
    setPortalError("");
    try {
      await fn();
      setPortalInfo("عملیات با موفقیت انجام شد.");
    } catch (error) {
      setPortalInfo("");
      setPortalError(error && error.message ? error.message : String(error));
    } finally {
      state.busy = false;
    }
  }

  async function handleAction(action, trigger) {
    const caseId = readCaseId();
    if (!caseId) throw new Error("شناسه پرونده تنظیم نشده است.");

    if (action === "post-comment") {
      const phase = Number(trigger.dataset.commentPhase);
      const prefix = trigger.dataset.commentPrefix || "";
      const message = readValue(prefix + "Comment");
      const isInternal = readChecked(prefix + "Internal");
      await postComment(caseId, phase, message, isInternal);
    } else if (action === "save-de1") {
      await state.panel.apiRequest({
        method: "PUT",
        path: casesPath("/" + caseId + "/data-entry1"),
        body: {
          startupTitle: readValue("de1Title"),
          businessDescription: readValue("de1Description"),
          requestedAmount: readNumber("de1Amount"),
          teamSize: readNumber("de1Team"),
          website: readValue("de1Website") || null,
          country: readValue("de1Country") || null,
          city: readValue("de1City") || null,
        },
      });
    } else if (action === "submit-de1") {
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/data-entry1/submit"),
        body: { comment: readValue("de1SubmitComment") || null },
      });
    } else if (action === "approve-de1") {
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/data-entry1/approve"),
        body: { comment: readValue("de1ApproveComment") || null },
      });
    } else if (action === "revise-de1") {
      const message = readValue("de1Revision");
      if (!message) throw new Error("پیام اصلاح الزامی است.");
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/data-entry1/revision-request"),
        body: { message },
      });
    } else if (action === "save-de2") {
      await state.panel.apiRequest({
        method: "PUT",
        path: casesPath("/" + caseId + "/data-entry2"),
        body: {
          marketAnalysis: readValue("de2Market"),
          revenueModel: readValue("de2Revenue"),
          competitiveAdvantage: readValue("de2Advantage"),
          financialProjection: readValue("de2Projection") || null,
        },
      });
    } else if (action === "submit-de2") {
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/data-entry2/submit"),
        body: { comment: readValue("de2SubmitComment") || null },
      });
    } else if (action === "approve-de2") {
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/data-entry2/approve"),
        body: { comment: readValue("de2ApproveComment") || null },
      });
    } else if (action === "revise-de2") {
      const message = readValue("de2Revision");
      if (!message) throw new Error("پیام اصلاح الزامی است.");
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/data-entry2/revision-request"),
        body: { message },
      });
    } else if (action === "record-valuation") {
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/valuations"),
        body: {
          type: readNumber("valuationType"),
          amount: readNumber("valuationAmount"),
          notes: readValue("valuationNotes") || null,
        },
      });
    } else if (action === "approve-val-initial") {
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/valuations/initial/approve"),
        body: { comment: readValue("valuationNotes") || null },
      });
    } else if (action === "approve-val-secondary") {
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/valuations/secondary/approve"),
        body: { comment: readValue("valuationNotes") || null },
      });
    } else if (action === "approve-pre-contract") {
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/contracts/preliminary/approve"),
        body: { comment: readValue("preContractApproveComment") || null },
      });
    } else if (action === "revise-pre-contract") {
      const message = readValue("preContractRevision");
      if (!message) throw new Error("پیام اصلاح الزامی است.");
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/contracts/preliminary/revision-request"),
        body: { message },
      });
    } else if (action === "finalize-contract") {
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/contracts/finalize-draft"),
        body: { comment: readValue("finalizeContractComment") || null },
      });
    } else if (action === "confirm-signature") {
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/contracts/confirm-signature"),
        body: { comment: readValue("confirmSignatureComment") || null },
      });
    } else if (action === "save-worksheet") {
      await state.panel.apiRequest({
        method: "PUT",
        path: casesPath("/" + caseId + "/financial-worksheet"),
        body: {
          bankName: readValue("wsBank"),
          iban: readValue("wsIban"),
          approvedAmount: readNumber("wsApproved") || null,
          paymentSchedule: readValue("wsSchedule") || null,
          notes: readValue("wsNotes") || null,
        },
      });
    } else if (action === "submit-worksheet") {
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/financial-worksheet/submit"),
        body: { comment: readValue("wsSubmitComment") || null },
      });
    } else if (action === "approve-worksheet") {
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/financial-worksheet/approve"),
        body: { comment: readValue("wsApproveComment") || null },
      });
    } else if (action === "revise-worksheet") {
      const message = readValue("wsRevision");
      if (!message) throw new Error("پیام اصلاح الزامی است.");
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/financial-worksheet/revision-request"),
        body: { message },
      });
    } else if (action === "record-payment") {
      const receiptInput = qs('input[data-payment-receipt="true"]');
      let receiptS3Key = null;
      if (receiptInput && receiptInput.files && receiptInput.files[0]) {
        receiptS3Key = await uploadDocument(caseId, 10, receiptInput.files[0]);
        receiptInput.value = "";
      }
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/payments"),
        body: {
          amount: readNumber("payAmount"),
          paymentDate: readValue("payDate"),
          transactionNumber: readValue("payTxn"),
          method: readNumber("payMethod"),
          status: readNumber("payStatus"),
          notes: null,
          receiptS3Key,
        },
      });
    } else if (action === "confirm-payment") {
      const paymentId = readValue("payId");
      if (!paymentId) throw new Error("شناسه پرداخت الزامی است.");
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/payments/" + encodeURIComponent(paymentId) + "/confirm"),
        body: null,
        json: false,
      });
    } else if (action === "cancel-payment") {
      const paymentId = readValue("payId");
      if (!paymentId) throw new Error("شناسه پرداخت الزامی است.");
      await state.panel.apiRequest({
        method: "POST",
        path: casesPath("/" + caseId + "/payments/" + encodeURIComponent(paymentId) + "/cancel"),
        body: null,
        json: false,
      });
    } else {
      return;
    }

    await refreshCase();
  }

  async function handleUpload(input) {
    const caseId = readCaseId();
    if (!caseId) throw new Error("شناسه پرونده تنظیم نشده است.");
    const file = input.files && input.files[0];
    if (!file) return;

    if (input.dataset.contractKind) {
      const commentField = input.dataset.contractCommentField;
      const commentPhase = Number(input.dataset.contractCommentPhase || model.phaseForStatus(pickStatus(state.caseData)));
      const comment = commentField ? readValue(commentField) : "";
      if (comment) await postComment(caseId, commentPhase, comment, isInternalSession());
      await uploadContract(caseId, input.dataset.contractKind, file);
    } else {
      const documentType = Number(input.dataset.uploadType);
      await uploadDocument(caseId, documentType, file);
    }
    const status = input.closest(".portal-upload-row__control")?.querySelector(".portal-upload-row__status");
    if (status) status.textContent = "بارگذاری شد: " + file.name;
    await refreshCase();
  }

  function wireEvents() {
    const refreshBtn = qs("#portalRefreshCase");
    if (refreshBtn) refreshBtn.addEventListener("click", () => withBusy(refreshCase));

    const unitTabs = qs("#portalUnitTabs");
    if (unitTabs) {
      unitTabs.addEventListener("click", (event) => {
        const btn = event.target.closest("[data-unit]");
        if (!btn) return;
        state.selectedUnit = btn.dataset.unit;
        render();
      });
    }

    const stageHost = qs("#portalStageHost");
    if (stageHost) {
      stageHost.addEventListener("click", (event) => {
        const btn = event.target.closest("[data-action]");
        if (!btn) return;
        withBusy(() => handleAction(btn.dataset.action, btn));
      });
      stageHost.addEventListener("change", (event) => {
        const input = event.target;
        if (!(input instanceof HTMLInputElement) || input.type !== "file") return;
        if (!input.dataset.uploadType && !input.dataset.contractKind) return;
        withBusy(() => handleUpload(input));
      });
    }

    document.addEventListener("testpanel:case-changed", () => {
      state.caseId = readCaseId();
      withBusy(refreshCase);
    });
  }

  window.initPortal = function initPortal(panel) {
    state.panel = panel;
    wireEvents();
    render();
    withBusy(refreshCase);
  };
})();
