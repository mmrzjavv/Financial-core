/* global WorkflowModel, GuaranteeWorkflowModel, LoanWorkflowModel */
(function () {
  function el(tag, cls, text) {
    const n = document.createElement(tag);
    if (cls) n.className = cls;
    if (text != null) n.textContent = text;
    return n;
  }

  function pick(obj, camel, pascal) {
    if (!obj) return "";
    const v = obj[camel] ?? obj[pascal];
    return v == null ? "" : v;
  }

  function formatDate(value) {
    if (!value) return "—";
    try {
      return new Date(value).toLocaleString("fa-IR");
    } catch {
      return String(value);
    }
  }

  function statusTitle(module, status) {
    const n = Number(status);
    if (module === "guarantee" && window.GuaranteeWorkflowModel) {
      const step = GuaranteeWorkflowModel.stepForStatus(n);
      return step ? step.title : String(status);
    }
    if (module === "loan" && window.LoanWorkflowModel) {
      const step = LoanWorkflowModel.stepForStatus(n);
      return step ? step.title : String(status);
    }
    if (window.WorkflowModel && WorkflowModel.STEPS) {
      const step = WorkflowModel.STEPS.find((s) => s.status === n);
      if (step) return step.title;
    }
    return String(status);
  }

  function renderCaseTable(host, rows, opts) {
    if (!host) return;
    host.innerHTML = "";
    if (!rows.length) {
      host.appendChild(el("div", "portal-empty", opts.emptyText || "پرونده‌ای یافت نشد."));
      return;
    }
    const wrap = el("div", "registry-table-wrap");
    const table = el("table", "registry-table");
    const thead = el("thead");
    const hr = el("tr");
    ["شماره پرونده", "موضوع", "متقاضی", "وضعیت", ""].forEach((h) => {
      const th = el("th", "", h);
      hr.appendChild(th);
    });
    thead.appendChild(hr);
    table.appendChild(thead);
    const tbody = el("tbody");
    rows.forEach((row) => {
      const tr = el("tr");
      tr.appendChild(el("td", "mono", row.caseNumber || "—"));
      tr.appendChild(el("td", "", row.subject || "—"));
      tr.appendChild(el("td", "", row.applicant || "—"));
      tr.appendChild(el("td", "", row.statusLabel || "—"));
      const tdAct = el("td");
      const btn = el("button", "btn btn--primary btn--small", "ورود به پرونده");
      btn.type = "button";
      btn.addEventListener("click", (e) => {
        e.stopPropagation();
        if (opts.onEnter) opts.onEnter(row);
      });
      tdAct.appendChild(btn);
      tr.appendChild(tdAct);
      tr.addEventListener("click", () => {
        if (opts.onEnter) opts.onEnter(row);
      });
      tbody.appendChild(tr);
    });
    table.appendChild(tbody);
    wrap.appendChild(table);
    host.appendChild(wrap);
  }

  function renderAttachments(host, docs, opts) {
    if (!host) return;
    host.innerHTML = "";
    const card = el("div", "card");
    card.appendChild(el("div", "card__title", "پیوست‌ها"));
    if (!docs.length) {
      card.appendChild(el("div", "muted", "هنوز فایلی بارگذاری نشده است."));
    } else {
      const list = el("div", "portal-doc-list");
      docs.forEach((doc) => {
        const item = el("div", "portal-doc-list__item portal-doc-list__item--row");
        const name = pick(doc, "fileName", "FileName") || "فایل";
        const type = pick(doc, "documentType", "DocumentType");
        const meta = el("div", "portal-doc-list__meta");
        meta.textContent = (opts.docTypeLabel ? opts.docTypeLabel(type) : "نوع " + type) + " — " + name;
        item.appendChild(meta);
        const id = pick(doc, "id", "Id");
        if (id && opts.onDownload) {
          const btn = el("button", "btn btn--small", "دانلود");
          btn.type = "button";
          btn.addEventListener("click", () => opts.onDownload(id));
          item.appendChild(btn);
        }
        list.appendChild(item);
      });
      card.appendChild(list);
    }
    if (opts.canUpload && opts.uploadSlot) {
      const zone = el("div", "card portal-card portal-card--nested");
      zone.appendChild(el("div", "card__title", "بارگذاری فایل"));
      zone.appendChild(opts.uploadSlot);
      card.appendChild(zone);
    }
    host.appendChild(card);
  }

  function renderTimeline(host, history, comments, opts) {
    if (!host) return;
    host.innerHTML = "";
    const card = el("div", "card");
    card.appendChild(el("div", "card__title", "تاریخچه گردش کار"));
    if (!history.length) {
      card.appendChild(el("div", "muted", "رویداد گردش کاری ثبت نشده است."));
    } else {
      const list = el("div", "portal-history");
      history.forEach((item) => {
        const row = el("div", "portal-history__item");
        const action = pick(item, "action", "Action");
        const from = pick(item, "fromStatus", "FromStatus");
        const to = pick(item, "toStatus", "ToStatus") || pick(item, "status", "Status");
        const at = formatDate(pick(item, "createdAt", "CreatedAt"));
        const actor = pick(item, "performedByName", "PerformedByName") || pick(item, "performedByUserId", "PerformedByUserId");
        row.innerHTML =
          "<strong>" +
          (action || "انتقال") +
          "</strong>" +
          (from || to ? " · " + [from, to].filter(Boolean).join(" → ") : "") +
          "<br/><span class='muted'>" +
          at +
          (actor ? " · " + actor : "") +
          "</span>";
        list.appendChild(row);
      });
      card.appendChild(list);
    }
    const cCard = el("div", "card portal-card portal-card--nested");
    cCard.appendChild(el("div", "card__title", "نظرات و درخواست‌های اصلاح"));
    if (!comments.length) {
      cCard.appendChild(el("div", "muted", "نظری ثبت نشده است."));
    } else {
      const thread = el("div", "portal-thread__list");
      comments.forEach((c) => {
        const item = el("div", "portal-thread__item");
        const meta = el("div", "portal-thread__meta");
        meta.textContent =
          formatDate(pick(c, "createdAt", "CreatedAt")) +
          (pick(c, "isInternal", "IsInternal") ? " · داخلی" : "");
        item.appendChild(meta);
        item.appendChild(el("div", "portal-thread__message", pick(c, "message", "Message") || "—"));
        thread.appendChild(item);
      });
      cCard.appendChild(thread);
    }
    if (opts && opts.onAddComment) {
      const row = el("div", "formrow");
      row.appendChild(el("label", "", "افزودن نظر"));
      const ta = document.createElement("textarea");
      ta.className = "portal-textarea";
      ta.rows = 3;
      row.appendChild(ta);
      cCard.appendChild(row);
      const btn = el("button", "btn btn--primary", "ثبت نظر");
      btn.type = "button";
      btn.addEventListener("click", () => {
        const msg = ta.value.trim();
        if (msg) opts.onAddComment(msg, () => {
          ta.value = "";
        });
      });
      cCard.appendChild(btn);
    }
    host.appendChild(card);
    host.appendChild(cCard);
  }

  window.UIComponents = {
    el,
    pick,
    formatDate,
    statusTitle,
    renderCaseTable,
    renderAttachments,
    renderTimeline,
  };
})();
