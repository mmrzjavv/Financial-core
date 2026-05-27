(function () {
  const UNITS = [
    { id: "all", label: "نمای کلی", roles: null },
    { id: "applicant", label: "متقاضی", roles: ["Applicant", "User", "Admin"] },
    { id: "investment", label: "سرمایه‌گذاری — کارشناس", roles: ["InvestmentExpert", "Admin"] },
    { id: "manager", label: "سرمایه‌گذاری — مدیر", roles: ["InvestmentManager", "Admin"] },
    { id: "legal", label: "حقوقی", roles: ["LegalExpert", "LegalManager", "LegalUnit", "Admin"] },
    { id: "financial", label: "مالی", roles: ["FinancialExpert", "FinancialManager", "FinancialUnit", "Admin"] },
    { id: "ceo", label: "مدیرعامل", roles: ["CEO", "Admin"] },
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
    { status: 20, key: "WaitingCeoApproval", title: "تأیید مدیرعامل", unit: "ceo", phase: 4 },
    { status: 15, key: "WaitingPayment", title: "پرداخت", unit: "financial", phase: 4 },
    { status: 16, key: "Completed", title: "تکمیل‌شده", unit: "all", phase: 5 },
    { status: 17, key: "Rejected", title: "رد شده", unit: "all", phase: 5 },
    { status: 18, key: "Cancelled", title: "لغو شده", unit: "all", phase: 5 },
    { status: 19, key: "Archived", title: "بایگانی", unit: "all", phase: 5 },
  ];

  /** فرم ورود اطلاعات ۱ — مطابق فرم متقاضی (نام شرکت/استارتاپ/موبایل در پروفایل Company است) */
  const DATA_ENTRY_1_DOCUMENTS = [
    { type: 1, label: "پیچ‌دک", hint: "ضروری — حداکثر ۱۰ مگابایت", required: true },
    { type: 11, label: "بیزینس پلن", hint: "اختیاری", required: false },
    { type: 99, label: "سایر فایل‌ها", hint: "اختیاری", required: false },
  ];

  const BUSINESS_STAGES = [
    { value: 1, label: "در مرحله ایده" },
    { value: 2, label: "دارای نمونه اولیه" },
  ];

  /** فرم ورود اطلاعات ۲ — مدارک و متن (اطلاعات شرکت/کاربر در پروفایل است) */
  const DATA_ENTRY_2_DOCUMENTS = [
    { type: 12, label: "اسناد معرفی شرکت (کاتالوگ، چارت سازمانی و …)", hint: "ضروری — ۱۰ مگابایت", required: true },
    { type: 13, label: "آخرین لیست بیمه کارکنان", hint: "ضروری — ۱۰ مگابایت", required: true },
    { type: 14, label: "اسکن آخرین تراز آزمایشی", hint: "ضروری — ۵ مگابایت", required: true },
    { type: 3, label: "اظهارنامه مالیاتی (حسابرسی‌شده در صورت وجود)", hint: "ضروری — ۱۰ مگابایت", required: true },
    { type: 15, label: "تصویر مجوزهای اصلی فعالیت شرکت", hint: "ضروری — ۵ مگابایت", required: true },
    { type: 4, label: "مدارک ثبت شرکت (آگهی تأسیس، روزنامه رسمی، آخرین تغییرات)", hint: "ضروری — ۱۰ مگابایت", required: true },
    { type: 19, label: "برنامه‌های جذب سرمایه (پرزنت و امکان‌سنجی)", hint: "ضروری — ۱۰ مگابایت", required: true },
    { type: 2, label: "صورت‌های مالی ۳ سال گذشته", hint: "اختیاری — ۱۰ مگابایت", required: false },
    { type: 6, label: "اسکن قراردادهای فروش و مستندات فروش", hint: "اختیاری — ۵ مگابایت", required: false },
    { type: 16, label: "جواز کسب، پروانه بهره‌برداری، کارت بازرگانی", hint: "اختیاری — ۱۰ مگابایت", required: false },
    { type: 17, label: "اعتبارسنجی مدیران و اعضای هیئت‌مدیره", hint: "اختیاری — ۱۰ مگابایت", required: false },
    { type: 18, label: "صورتجلسه هیئت‌مدیره", hint: "اختیاری — ۱۰ مگابایت", required: false },
  ];

  /** سازگاری قدیمی — از DATA_ENTRY_2 استفاده کنید */
  const APPLICANT_DOCUMENTS = DATA_ENTRY_2_DOCUMENTS;

  const ROLE_ALIASES = {
    User: "Applicant",
    LegalUnit: "LegalExpert",
    FinancialUnit: "FinancialExpert",
    InvestmentUnit: "InvestmentExpert",
  };

  const ROLE_BY_NUMBER = {
    1: "Applicant",
    10: "InvestmentExpert",
    11: "InvestmentManager",
    12: "CEO",
    20: "LegalExpert",
    21: "LegalManager",
    30: "FinancialExpert",
    31: "FinancialManager",
    50: "CreditExpert",
    51: "CreditManager",
    40: "TechnicalExpert",
    41: "TechnicalManager",
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

  const TERMINAL_STATUSES = new Set([17, 18, 19]);

  function getStepperSteps() {
    return STEPS.filter((step) => !TERMINAL_STATUSES.has(step.status));
  }

  function getStepOrderIndex(status) {
    const value = Number(status);
    return getStepperSteps().findIndex((step) => step.status === value);
  }

  function compareStepOrder(a, b) {
    const ai = getStepOrderIndex(a);
    const bi = getStepOrderIndex(b);
    if (ai >= 0 && bi >= 0) return ai - bi;
    return Number(a) - Number(b);
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
    if (value <= 15 || value === 20) return 4;
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
    DATA_ENTRY_1_DOCUMENTS,
    DATA_ENTRY_2_DOCUMENTS,
    BUSINESS_STAGES,
    APPLICANT_DOCUMENTS,
    normalizeRole,
    getStep,
    getStepperSteps,
    getStepOrderIndex,
    compareStepOrder,
    getUnit,
    roleMatchesUnit,
    canActOnCase,
    phaseForStatus,
    isInternalRole,
  };
})();
