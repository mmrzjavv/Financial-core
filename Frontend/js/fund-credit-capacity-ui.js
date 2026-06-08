(function () {
  function pick(obj, camel, pascal) {
    if (!obj) return undefined;
    if (obj[camel] !== undefined) return obj[camel];
    if (obj[pascal] !== undefined) return obj[pascal];
    return undefined;
  }

  function formatRialAmount(value) {
    if (value == null || value === "") return "—";
    const n = Number(value);
    if (!Number.isFinite(n)) return String(value);
    return n.toLocaleString("fa-IR") + " ریال";
  }

  function el(tag, cls, text) {
    const n = document.createElement(tag);
    if (cls) n.className = cls;
    if (text != null) n.textContent = text;
    return n;
  }

  function canViewFundCreditCapacity(role) {
    return ["CEO", "Admin", "TechnicalExpert"].includes(role);
  }

  function renderFundCreditCapacityWidget(card, caseData, role) {
    if (!canViewFundCreditCapacity(role)) return;

    const cap = pick(caseData, "fundCreditCapacity", "FundCreditCapacity");
    const wrap = el("div", "card portal-card portal-card--nested fund-capacity-card");
    wrap.appendChild(el("div", "card__title", "وضعیت ظرفیت اعتبار دوره‌ای صندوق"));

    if (!cap) {
      wrap.appendChild(el("div", "muted", "داده ظرفیت برای این مرحله در دسترس نیست."));
      card.appendChild(wrap);
      return;
    }

    const budget = pick(cap, "totalPeriodAllocation", "TotalPeriodAllocation");
    const used = pick(cap, "totalUtilized", "TotalUtilized");
    const remaining = pick(cap, "remainingCapacity", "RemainingCapacity");
    const periodStart = pick(cap, "periodStart", "PeriodStart");
    const expiresAt = pick(cap, "expiresAt", "ExpiresAt");

    if (budget == null) {
      wrap.appendChild(
        el(
          "div",
          "muted",
          "سقف دوره‌ای فعالی برای این ماژول تعریف نشده است — از داشبورد «سقف اعتبار دوره‌ای» تنظیم کنید."
        )
      );
      card.appendChild(wrap);
      return;
    }

    const budgetNum = Number(budget) || 0;
    const usedNum = Number(used) || 0;
    const pct = budgetNum > 0 ? Math.min(100, Math.round((usedNum / budgetNum) * 100)) : 0;

    wrap.appendChild(
      el(
        "div",
        "fund-capacity-card__summary",
        "بودجه: " +
          formatRialAmount(budget) +
          " | مصرف: " +
          formatRialAmount(used) +
          " | مانده: " +
          formatRialAmount(remaining)
      )
    );

    if (periodStart || expiresAt) {
      wrap.appendChild(
        el("div", "muted fund-capacity-card__period", "بازه: " + (periodStart || "—") + " تا " + (expiresAt || "—"))
      );
    }

    const barTrack = el("div", "fund-capacity-bar");
    const barFill = el("div", "fund-capacity-bar__fill");
    barFill.style.width = pct + "%";
    barTrack.appendChild(barFill);
    wrap.appendChild(barTrack);
    wrap.appendChild(el("div", "muted fund-capacity-card__pct", "مصرف " + pct + "% از سقف دوره"));

    card.appendChild(wrap);
  }

  window.FundCreditCapacityUi = {
    canViewFundCreditCapacity,
    renderFundCreditCapacityWidget,
    formatRialAmount,
    pick,
  };
})();
