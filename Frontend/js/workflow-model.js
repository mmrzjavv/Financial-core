(function () {
  const UNITS = [
    { id: "all", label: "نمای کلی", roles: null },
    { id: "applicant", label: "متقاضی", roles: ["Applicant", "User", "Admin"] },
    { id: "investment", label: "واحد سرمایه‌گذاری", roles: ["InvestmentExpert", "Admin"] },
    { id: "manager", label: "مدیریت سرمایه‌گذاری", roles: ["InvestmentManager", "Admin"] },
    { id: "legal", label: "واحد حقوقی", roles: ["LegalExpert", "LegalUnit", "Admin"] },
    { id: "financial", label: "واحد مالی", roles: ["FinancialExpert", "FinancialUnit", "Admin"] },
  ];

  const PHASES = {
    1: "درخواست",
    2: "ارزش‌گذاری",
    3: "حقوقی",
    4: "مالی",
    5: "اختتام",
  };

  const STEPS = [
    { status: 1, key: "Draft", title: "پیش‌نویس", unit: "applicant", phase: 1 },
    { status: 2, key: "DataEntry1", title: "فرم اولیه", unit: "applicant", phase: 1 },
    { status: 3, key: "ReviewDataEntry1", title: "بررسی فرم اولیه", unit: "investment", phase: 1 },
    { status: 4, key: "DataEntry2", title: "فرم تکمیلی", unit: "applicant", phase: 1 },
    { status: 5, key: "ReviewDataEntry2", title: "بررسی فرم تکمیلی", unit: "investment", phase: 1 },
    { status: 6, key: "InitialValuation", title: "ارزش‌گذاری اولیه", unit: "manager", phase: 2 },
    { status: 7, key: "SecondaryValuation", title: "ارزش‌گذاری ثانویه", unit: "manager", phase: 2 },
    { status: 8, key: "WaitingPreliminaryContract", title: "آپلود پیش‌قرارداد", unit: "legal", phase: 3 },
    { status: 9, key: "WaitingUserReviewPreliminaryContract", title: "بازبینی پیش‌قرارداد", unit: "applicant", phase: 3 },
    { status: 10, key: "ContractDrafting", title: "تدوین قرارداد", unit: "legal", phase: 3 },
    { status: 11, key: "WaitingContractSignature", title: "امضای قرارداد", unit: "legal", phase: 3 },
    { status: 12, key: "WaitingSignedContractUpload", title: "آپلود قرارداد امضاشده", unit: "legal", phase: 3 },
    { status: 13, key: "WaitingFinancialWorksheet", title: "کاربرگ مالی", unit: "investment", phase: 4 },
    { status: 14, key: "FinancialWorksheetReview", title: "بررسی کاربرگ مالی", unit: "financial", phase: 4 },
    { status: 15, key: "WaitingPayment", title: "پرداخت", unit: "financial", phase: 4 },
    { status: 16, key: "Completed", title: "تکمیل‌شده", unit: "all", phase: 5 },
    { status: 17, key: "Rejected", title: "رد شده", unit: "all", phase: 5 },
    { status: 18, key: "Cancelled", title: "لغو شده", unit: "all", phase: 5 },
    { status: 19, key: "Archived", title: "بایگانی", unit: "all", phase: 5 },
  ];

  const APPLICANT_DOCUMENTS = [
    { type: 1, label: "پیچ‌دک", hint: "PitchDeck" },
    { type: 2, label: "صورت‌های مالی", hint: "FinancialStatements" },
    { type: 3, label: "مدارک مالیاتی", hint: "TaxDocuments" },
    { type: 4, label: "ثبت شرکت", hint: "CompanyRegistration" },
    { type: 5, label: "سهامداران و مدیران", hint: "ShareholderManager" },
    { type: 6, label: "اسناد فروش", hint: "SalesDocuments" },
    { type: 99, label: "سایر", hint: "Other" },
  ];

  const ROLE_ALIASES = {
    User: "Applicant",
    LegalUnit: "LegalExpert",
    FinancialUnit: "FinancialExpert",
  };

  const ROLE_BY_NUMBER = {
    1: "Applicant",
    10: "InvestmentExpert",
    11: "InvestmentManager",
    12: "CEO",
    20: "LegalExpert",
    30: "FinancialExpert",
    40: "TechnicalExpert",
    100: "Admin",
  };

  function normalizeRole(roleText, roleNumber) {
    if (roleNumber != null && Number.isFinite(Number(roleNumber))) {
      const mapped = ROLE_BY_NUMBER[Number(roleNumber)];
      if (mapped) return mapped;
    }

    const role = String(roleText || "").trim();
    if (!role) return "";

    const numericRole = Number(role);
    if (Number.isFinite(numericRole) && ROLE_BY_NUMBER[numericRole]) {
      return ROLE_BY_NUMBER[numericRole];
    }

    return ROLE_ALIASES[role] || role;
  }

  function getStep(status) {
    const value = Number(status);
    return STEPS.find((step) => step.status === value) || null;
  }

  function getUnit(unitId) {
    return UNITS.find((unit) => unit.id === unitId) || null;
  }

  function roleMatchesUnit(roleText, unitId) {
    const unit = getUnit(unitId);
    if (!unit || unit.id === "all") return true;
    const role = normalizeRole(roleText);
    return unit.roles.some((allowed) => allowed === role);
  }

  function canActOnCase(roleText, unitId) {
    if (normalizeRole(roleText) === "Admin") return true;
    return roleMatchesUnit(roleText, unitId);
  }

  function phaseForStatus(status) {
    const value = Number(status);
    if (value <= 5) return 1;
    if (value <= 7) return 2;
    if (value <= 12) return 3;
    if (value <= 15) return 4;
    return 5;
  }

  function isInternalRole(roleText) {
    const role = normalizeRole(roleText);
    return role !== "Applicant" && role !== "User";
  }

  window.WorkflowModel = {
    UNITS,
    PHASES,
    STEPS,
    APPLICANT_DOCUMENTS,
    normalizeRole,
    getStep,
    getUnit,
    roleMatchesUnit,
    canActOnCase,
    phaseForStatus,
    isInternalRole,
  };
})();
