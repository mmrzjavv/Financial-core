/* global WorkflowModel */
(function () {
  const MODULE = { Investment: 1, Guarantee: 2, GuaranteeRenewal: 3, Loan: 4 };

  const STEPS = {
    1: { id: 1, title: "پیش‌نویس", unit: "applicant", phase: 1 },
    2: { id: 2, title: "ورود اطلاعات", unit: "applicant", phase: 1 },
    3: { id: 3, title: "بررسی اعتبارات", unit: "credit", phase: 1 },
    4: { id: 4, title: "اصلاح (اعتبارات)", unit: "applicant", phase: 1 },
    5: { id: 5, title: "تأیید مدیرعامل (اول)", unit: "ceo", phase: 2 },
    6: { id: 6, title: "لغو مدیرعامل", unit: "ceo", phase: 2 },
    7: { id: 7, title: "قرارداد خام و اقساط", unit: "legal", phase: 3 },
    8: { id: 8, title: "امضای متقاضی", unit: "applicant", phase: 3 },
    9: { id: 9, title: "بررسی حقوقی", unit: "legal", phase: 3 },
    10: { id: 10, title: "اصلاح (حقوقی)", unit: "applicant", phase: 3 },
    11: { id: 11, title: "بررسی مالی", unit: "financial", phase: 4 },
    12: { id: 12, title: "اصلاح (مالی)", unit: "applicant", phase: 4 },
    13: { id: 13, title: "قرارداد نهایی", unit: "legal", phase: 3 },
    14: { id: 14, title: "تأیید مدیرعامل (نهایی)", unit: "ceo", phase: 4 },
    15: { id: 15, title: "آماده پرداخت", unit: "financial", phase: 4 },
    16: { id: 16, title: "فاز بازپرداخت", unit: "applicant", phase: 5 },
    17: { id: 17, title: "تکمیل", unit: "all", phase: 6 },
    18: { id: 18, title: "بایگانی", unit: "all", phase: 6 },
  };

  const DATA_ENTRY_DOCUMENTS = [
    { type: 1, label: "فرم پیوست الحاقی", required: true },
    { type: 2, label: "اسناد مورد نیاز صندوق و مشخصات شرکت", required: true },
    { type: 3, label: "آگهی تأسیس و آخرین روزنامه رسمی", required: true },
    { type: 4, label: "کارت ملی و شناسنامه مدیرعامل و هیئت مدیره", required: true },
    { type: 5, label: "قراردادهای مالی و اعتباری", required: true },
    { type: 6, label: "صورت‌های مالی", required: true },
    { type: 7, label: "گردش/معدل حساب‌های بانکی", required: true },
    { type: 8, label: "فرم درخواست اصلی", required: true },
    { type: 9, label: "قرارداد پیشنهادی تأمین مالی", required: true },
    { type: 10, label: "رسید بیمه کارکنان", required: false },
    { type: 11, label: "مجوزهای فعالیت", required: false },
    { type: 12, label: "استعلام اعتبارسنجی", required: false },
  ];

  const APPLICANT_CATEGORIES = [
    { value: 1, label: "دانش‌بنیان" },
    { value: 2, label: "خلاق" },
    { value: 4, label: "فناور" },
    { value: 8, label: "سایر" },
  ];

  const FACILITY_TYPES = [
    { value: 1, label: "قرض‌الحسنه" },
    { value: 2, label: "جعاله" },
    { value: 3, label: "مشارکت" },
    { value: 4, label: "فروش اقساطی" },
    { value: 5, label: "اجاره به شرط تملیک" },
  ];

  const PHASES = {
    1: "درخواست",
    2: "اعتبارات",
    3: "حقوقی",
    4: "مالی",
    5: "بازپرداخت",
    6: "اختتام",
  };

  const UNITS = [
    { id: "all", label: "همه" },
    { id: "applicant", label: "متقاضی", roles: ["Applicant", "User", "Admin"] },
    { id: "credit", label: "اعتبارات", roles: ["CreditExpert", "CreditManager", "Admin"] },
    { id: "legal", label: "حقوقی", roles: ["LegalExpert", "LegalManager", "LegalUnit", "Admin"] },
    { id: "financial", label: "مالی", roles: ["FinancialExpert", "FinancialManager", "FinancialUnit", "Admin"] },
    { id: "ceo", label: "مدیرعامل", roles: ["CEO", "Admin"] },
  ];

  function getUnit(unitId) {
    return UNITS.find((u) => u.id === unitId) || null;
  }

  function getStepperSteps() {
    return Object.values(STEPS)
      .filter((step) => step.id !== 6)
      .sort((a, b) => a.id - b.id);
  }

  function getStepOrderIndex(status) {
    const value = Number(status);
    return getStepperSteps().findIndex((step) => step.id === value);
  }

  function normalizeRole(userRoleText, userRoleNumber) {
    if (window.WorkflowModel && typeof window.WorkflowModel.normalizeRole === "function") {
      return window.WorkflowModel.normalizeRole(userRoleText, userRoleNumber);
    }
    return userRoleText || "";
  }

  function stepForStatus(status) {
    return STEPS[Number(status)] || { title: "نامشخص", unit: "all", phase: 0 };
  }

  function canActOnCase(status, role) {
    const step = stepForStatus(status);
    const unit = step.unit;
    if (unit === "all") return false;
    const map = {
      applicant: ["Applicant", "User", "Admin"],
      credit: ["CreditExpert", "CreditManager", "Admin"],
      legal: ["LegalExpert", "LegalManager", "Admin"],
      financial: ["FinancialExpert", "FinancialManager", "Admin"],
      ceo: ["CEO", "Admin"],
    };
    return (map[unit] || []).includes(role);
  }

  const WORKFLOW_DOCUMENTS = [
    { type: 13, label: "قرارداد خام" },
    { type: 14, label: "جدول اقساط" },
    { type: 15, label: "قرارداد امضاشده" },
    { type: 16, label: "پیوست ۱" },
    { type: 17, label: "پیوست ۲" },
    { type: 18, label: "پیوست ۳" },
    { type: 19, label: "پیوست ۴" },
    { type: 20, label: "پیوست ۵" },
    { type: 21, label: "پیوست ۶" },
    { type: 22, label: "قرارداد نهایی" },
    { type: 23, label: "رسید پرداخت" },
    { type: 99, label: "سایر" },
  ];

  function documentTypeLabel(type) {
    const t = Number(type);
    const fromEntry = DATA_ENTRY_DOCUMENTS.find((d) => d.type === t);
    if (fromEntry) return fromEntry.label;
    const fromWorkflow = WORKFLOW_DOCUMENTS.find((d) => d.type === t);
    if (fromWorkflow) return fromWorkflow.label;
    return "مدرک (نوع " + t + ")";
  }

  window.LoanWorkflowModel = {
    MODULE,
    STEPS,
    PHASES,
    UNITS,
    DATA_ENTRY_DOCUMENTS,
    APPLICANT_CATEGORIES,
    FACILITY_TYPES,
    normalizeRole,
    getUnit,
    getStepperSteps,
    getStepOrderIndex,
    stepForStatus,
    canActOnCase,
    documentTypeLabel,
  };
})();
