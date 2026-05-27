/* global WorkflowModel */
(function () {
  const MODULE = { Investment: 1, Guarantee: 2, GuaranteeRenewal: 3 };

  const STEPS = {
    1: { id: 1, title: "پیش‌نویس", unit: "applicant", phase: 1 },
    2: { id: 2, title: "ورود اطلاعات", unit: "applicant", phase: 1 },
    3: { id: 3, title: "بررسی اعتبارات", unit: "credit", phase: 2 },
    4: { id: 4, title: "فرم تصویب", unit: "credit", phase: 2 },
    5: { id: 5, title: "تأیید مدیرعامل (اول)", unit: "ceo", phase: 2 },
    6: { id: 6, title: "پیش‌قرارداد", unit: "legal", phase: 3 },
    7: { id: 7, title: "امضا و پیوست", unit: "applicant", phase: 3 },
    8: { id: 8, title: "بررسی مالی", unit: "financial", phase: 4 },
    9: { id: 9, title: "قرارداد نهایی", unit: "legal", phase: 3 },
    10: { id: 10, title: "تأیید مدیرعامل (نهایی)", unit: "ceo", phase: 4 },
    11: { id: 11, title: "صدور", unit: "financial", phase: 4 },
    12: { id: 12, title: "تکمیل", unit: "all", phase: 5 },
  };

  /** مدارک ورود اطلاعات — مطابق mockup ضمانت‌نامه */
  const DATA_ENTRY_DOCUMENTS = [
    { type: 8, label: "فیش مبلغ تشکیل پرونده", hint: "ضروری — حداکثر ۱۰ مگابایت", required: true },
    { type: 9, label: "فرم پیوست جهت تشکیل پرونده", hint: "اختیاری", required: false },
    { type: 7, label: "فرم دریافت اطلاعات اعتباری (حقوقی و حقیقی)", hint: "ضروری", required: true },
    { type: 10, label: "اسناد معرفی شرکت (تابلو، چارت سازمانی و …)", hint: "اختیاری", required: false },
    { type: 2, label: "آگهی تأسیس و آخرین روزنامه رسمی هیئت‌مدیره", hint: "ضروری", required: true },
    { type: 12, label: "کارت ملی و شناسنامه مدیرعامل و اعضای هیئت‌مدیره", hint: "ضروری", required: true },
    { type: 11, label: "قرارداد اجاره یا سند مالکیت محل شرکت", hint: "اختیاری", required: false },
    { type: 3, label: "صورت‌های مالی ۳ سال گذشته", hint: "ضروری", required: true },
    { type: 5, label: "گردش حساب‌های بانکی فعال شرکت (یک سال گذشته)", hint: "ضروری", required: true },
    { type: 4, label: "تصویر مجوزهای اصلی فعالیت", hint: "ضروری", required: true },
    { type: 13, label: "گزارش امکان‌سنجی فنی و اقتصادی (FS یا BP)", hint: "اختیاری", required: false },
    { type: 15, label: "اسکن قراردادهای فروش و تأییدیه‌ها", hint: "اختیاری", required: false },
    { type: 14, label: "فرم درخواست صدور ضمانت‌نامه", hint: "ضروری", required: true },
    { type: 6, label: "استعلام اعتباری شرکت و مدیران (سامانه ایران)", hint: "اختیاری", required: false },
    { type: 16, label: "تصویر آگهی مناقصه/مزایده", hint: "ضروری — برای نوع «شرکت در مناقصه»", required: false, whenGuaranteeType: 1 },
    { type: 17, label: "تصویر قرارداد پایه", hint: "ضروری برای حسن انجام کار / پیش‌پرداخت", required: false, whenGuaranteeTypes: [2, 3] },
  ];

  const GUARANTEE_TYPES = [
    { value: 1, label: "شرکت در مناقصه" },
    { value: 2, label: "حسن انجام کار" },
    { value: 3, label: "پیش‌پرداخت" },
    { value: 4, label: "تعهد پرداخت" },
    { value: 99, label: "سایر" },
  ];

  const APPLICANT_CATEGORIES = [
    { value: 1, label: "دانش‌بنیان" },
    { value: 2, label: "خلاق" },
    { value: 4, label: "فناور" },
    { value: 8, label: "سایر" },
  ];

  const APPLICANT_LEGAL_FORMS = [
    { value: 1, label: "دولتی" },
    { value: 2, label: "خصوصی" },
    { value: 3, label: "دانش‌بنیان" },
    { value: 4, label: "تعاونی" },
    { value: 99, label: "سایر" },
  ];

  const BENEFICIARY_COMPANY_TYPES = [
    { value: 1, label: "دولتی" },
    { value: 2, label: "عمومی غیردولتی" },
    { value: 3, label: "خصوصی" },
    { value: 99, label: "سایر" },
  ];

  const PHASES = {
    1: "درخواست",
    2: "اعتبارات",
    3: "حقوقی",
    4: "مالی",
    5: "اختتام",
  };

  /** مدارک مراحل گردش کار (غیر از ورود اطلاعات) */
  const WORKFLOW_STAGE_DOCUMENTS = {
    6: {
      title: "بارگذاری پیش‌قرارداد",
      subtitle: "واحد حقوقی — قرارداد خام",
      autoAdvanceHint: "پس از بارگذاری موفق، پرونده معمولاً خودکار به مرحله «امضا و پیوست» می‌رود.",
      docs: [{ type: 18, label: "پیش‌قرارداد (قرارداد خام)", hint: "PDF یا Word", required: true }],
    },
    7: {
      title: "بارگذاری قرارداد امضاشده و پیوست‌ها",
      subtitle: "متقاضی",
      autoAdvanceHint: "قرارداد امضاشده الزامی است؛ پس از بارگذاری «ارسال قرارداد امضاشده» را بزنید.",
      docs: [
        { type: 19, label: "قرارداد امضاشده", hint: "ضروری", required: true },
        { type: 20, label: "پیوست امضاشده ۱", required: false },
        { type: 21, label: "پیوست امضاشده ۲", required: false },
        { type: 22, label: "پیوست امضاشده ۳", required: false },
        { type: 23, label: "پیوست امضاشده ۴", required: false },
        { type: 24, label: "پیوست امضاشده ۵", required: false },
        { type: 25, label: "پیوست امضاشده ۶", required: false },
      ],
    },
    9: {
      title: "بارگذاری قرارداد نهایی",
      subtitle: "واحد حقوقی",
      autoAdvanceHint: "پس از بارگذاری موفق، پرونده معمولاً خودکار به تأیید مدیرعامل (نهایی) می‌رود.",
      docs: [{ type: 26, label: "قرارداد نهایی", hint: "ضروری", required: true }],
    },
    11: {
      title: "بارگذاری مدارک صدور",
      subtitle: "واحد مالی",
      autoAdvanceHint: "هر دو مدرک الزامی است؛ سپس «تأیید صدور» را بزنید.",
      docs: [
        { type: 27, label: "ضمانت‌نامه صادره", required: true },
        { type: 28, label: "رسید صدور", required: true },
      ],
    },
  };

  const UNITS = [
    { id: "all", label: "همه" },
    { id: "applicant", label: "متقاضی", roles: ["Applicant", "User", "Admin"] },
    { id: "credit", label: "اعتبارات", roles: ["CreditExpert", "CreditManager", "Admin"] },
    { id: "legal", label: "حقوقی", roles: ["LegalExpert", "LegalManager", "LegalUnit", "Admin"] },
    { id: "financial", label: "مالی", roles: ["FinancialExpert", "FinancialManager", "FinancialUnit", "Admin"] },
    { id: "ceo", label: "مدیرعامل", roles: ["CEO", "Admin"] },
  ];

  function normalizeRole(roleText, roleNumber) {
    if (window.WorkflowModel && typeof window.WorkflowModel.normalizeRole === "function") {
      return window.WorkflowModel.normalizeRole(roleText, roleNumber);
    }
    return String(roleText || "").trim();
  }

  function getUnit(unitId) {
    return UNITS.find((u) => u.id === unitId) || null;
  }

  function roleMatchesUnit(role, unitId) {
    const unit = getUnit(unitId);
    if (!unit || unit.id === "all") return true;
    const normalized = normalizeRole(role);
    return (unit.roles || []).some((allowed) => allowed === normalized);
  }

  function canActOnCase(role, unitId) {
    const normalized = normalizeRole(role);
    if (normalized === "Admin") return true;
    return roleMatchesUnit(normalized, unitId);
  }

  function unitRoleLabels(unitId) {
    const unit = getUnit(unitId);
    if (!unit) return "";
    const labels = {
      applicant: "متقاضی (Applicant)",
      credit: "واحد اعتبارات (CreditExpert / CreditManager)",
      legal: "واحد حقوقی (LegalExpert / LegalManager)",
      financial: "واحد مالی (FinancialExpert / FinancialManager)",
      ceo: "مدیرعامل (CEO)",
    };
    return labels[unit.id] || unit.label;
  }

  const GUARANTEE_TYPE_BY_NAME = {
    tender: 1,
    performancebond: 2,
    advancepayment: 3,
    paymentcommitment: 4,
    other: 99,
  };

  const DOCUMENT_TYPE_BY_NAME = {
    requestletter: 1,
    establishmentgazette: 2,
    financialstatements3years: 3,
    activitylicenses: 4,
    bankaccountturnover: 5,
    creditinquiryresult: 6,
    creditinformationform: 7,
    caseformationfeereceipt: 8,
    caseformationattachmentform: 9,
    companyintroductiondocs: 10,
    leaseorownershipdeed: 11,
    ceoboardidcards: 12,
    feasibilityreport: 13,
    guaranteeissuancerequestform: 14,
    salescontractsscan: 15,
    tenderannouncement: 16,
    basecontractimage: 17,
  };

  function normalizeDocumentType(value) {
    if (value == null || value === "") return NaN;
    if (typeof value === "number" && Number.isFinite(value)) return value;
    const s = String(value).trim();
    if (/^\d+$/.test(s)) return Number(s);
    const key = s.replace(/[\s_-]/g, "").toLowerCase();
    return DOCUMENT_TYPE_BY_NAME[key] ?? NaN;
  }

  function normalizeGuaranteeType(value) {
    if (value == null || value === "") return 0;
    if (typeof value === "number" && Number.isFinite(value)) return value;
    const s = String(value).trim();
    if (/^\d+$/.test(s)) return Number(s);
    const key = s.replace(/[\s_-]/g, "").toLowerCase();
    return GUARANTEE_TYPE_BY_NAME[key] || 0;
  }

  function documentsForGuaranteeType(guaranteeType) {
    const gt = normalizeGuaranteeType(guaranteeType);
    return DATA_ENTRY_DOCUMENTS.filter((doc) => {
      if (doc.whenGuaranteeType != null) return doc.whenGuaranteeType === gt;
      if (doc.whenGuaranteeTypes) return doc.whenGuaranteeTypes.includes(gt);
      return true;
    });
  }

  function requiredDocumentsForSubmit(guaranteeType) {
    const gt = normalizeGuaranteeType(guaranteeType);
    return DATA_ENTRY_DOCUMENTS.filter((doc) => {
      if (doc.whenGuaranteeType != null) return doc.whenGuaranteeType === gt;
      if (doc.whenGuaranteeTypes) return doc.whenGuaranteeTypes.includes(gt);
      return doc.required;
    });
  }

  function isDocRequiredForType(doc, guaranteeType) {
    const gt = normalizeGuaranteeType(guaranteeType);
    if (!gt) return !!doc.required;
    if (doc.required) return true;
    if (doc.whenGuaranteeType != null) return doc.whenGuaranteeType === gt;
    if (doc.whenGuaranteeTypes) return doc.whenGuaranteeTypes.includes(gt);
    return false;
  }

  /** همه ردیف‌های بارگذاری: نوع ذخیره‌شده + انتخاب فرم + هر مدرک الزامی */
  function uploadDocumentDefs(savedGuaranteeType, formGuaranteeType) {
    const saved = normalizeGuaranteeType(savedGuaranteeType);
    const form = normalizeGuaranteeType(formGuaranteeType);
    const types = new Set([saved, form].filter((t) => t > 0));
    const map = new Map();
    types.forEach((gt) => {
      documentsForGuaranteeType(gt).forEach((d) => map.set(d.type, d));
    });
    const validateGt = saved || form;
    if (validateGt > 0) {
      requiredDocumentsForSubmit(validateGt).forEach((d) => map.set(d.type, d));
    }
    if (map.size === 0) {
      DATA_ENTRY_DOCUMENTS.forEach((d) => map.set(d.type, d));
    }
    const priority = [16, 17, 8, 7, 2, 12, 3, 5, 4, 14, 6, 9, 10, 11, 13, 15];
    return Array.from(map.values()).sort((a, b) => {
      const ia = priority.indexOf(a.type);
      const ib = priority.indexOf(b.type);
      if (ia === -1 && ib === -1) return a.type - b.type;
      if (ia === -1) return 1;
      if (ib === -1) return -1;
      return ia - ib;
    });
  }

  function documentTypeLabel(type) {
    const t = normalizeDocumentType(type);
    if (!Number.isFinite(t)) return "مدرک";
    const fromEntry = DATA_ENTRY_DOCUMENTS.find((d) => d.type === t);
    if (fromEntry) return fromEntry.label;
    for (const stage of Object.values(WORKFLOW_STAGE_DOCUMENTS)) {
      const found = stage.docs.find((d) => d.type === t);
      if (found) return found.label;
    }
    const extra = {
      1: "نامه درخواست",
      6: "استعلام اعتباری",
      18: "پیش‌قرارداد (قرارداد خام)",
      19: "قرارداد امضاشده",
      20: "پیوست امضاشده ۱",
      21: "پیوست امضاشده ۲",
      22: "پیوست امضاشده ۳",
      23: "پیوست امضاشده ۴",
      24: "پیوست امضاشده ۵",
      25: "پیوست امضاشده ۶",
      26: "قرارداد نهایی",
      27: "ضمانت‌نامه صادره",
      28: "رسید صدور",
      99: "سایر",
    };
    return extra[t] || "مدرک (نوع " + t + ")";
  }

  window.GuaranteeWorkflowModel = {
    MODULE,
    STEPS,
    DATA_ENTRY_DOCUMENTS,
    WORKFLOW_STAGE_DOCUMENTS,
    GUARANTEE_TYPES,
    APPLICANT_CATEGORIES,
    APPLICANT_LEGAL_FORMS,
    BENEFICIARY_COMPANY_TYPES,
    UNITS,
    normalizeRole,
    roleMatchesUnit,
    canActOnCase,
    unitRoleLabels,
    normalizeGuaranteeType,
    normalizeDocumentType,
    documentTypeLabel,
    documentsForGuaranteeType,
    requiredDocumentsForSubmit,
    isDocRequiredForType,
    uploadDocumentDefs,
    PHASES,
    getUnit(unitId) {
      return UNITS.find((u) => u.id === unitId) || null;
    },
    getStepperSteps() {
      return Object.values(STEPS).sort((a, b) => a.id - b.id);
    },
    getStepOrderIndex(status) {
      const value = Number(status);
      const steps = Object.values(STEPS).sort((a, b) => a.id - b.id);
      return steps.findIndex((step) => step.id === value);
    },
    stepForStatus(status) {
      return STEPS[status] || { id: status, title: "نامشخص", unit: "all", phase: 0 };
    },
  };
})();
