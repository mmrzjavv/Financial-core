// Minimal, runtime-editable configuration.
(function () {
  const DEFAULTS = {
    baseUrl: "http://localhost:5081",
    casesVersion: "1",
    devOtp: "123456",
    storageKey: "workflow_test_panel.config.v1",
    workflowPersonas: [
      { key: "admin", phone: "09100000001", role: 100, firstName: "Admin", lastName: "Tester", label: "Admin" },
      { key: "applicant", phone: "09100000002", role: 1, firstName: "Applicant", lastName: "Tester", label: "Applicant" },
      { key: "investmentExpert", phone: "09100000003", role: 10, firstName: "Investment", lastName: "Expert", label: "InvestmentExpert" },
      { key: "investmentManager", phone: "09100000004", role: 11, firstName: "Investment", lastName: "Manager", label: "InvestmentManager" },
      { key: "legalExpert", phone: "09100000005", role: 20, firstName: "Legal", lastName: "Expert", label: "LegalExpert" },
      { key: "financialExpert", phone: "09100000006", role: 30, firstName: "Financial", lastName: "Expert", label: "FinancialExpert" },
    ],
  };

  function safeParseJson(raw, fallback) {
    try {
      if (!raw) return fallback;
      return JSON.parse(raw);
    } catch {
      return fallback;
    }
  }

  const stored = safeParseJson(localStorage.getItem(DEFAULTS.storageKey), {});
  const cfg = {
    ...DEFAULTS,
    ...stored,
  };

  cfg.baseUrl = String(cfg.baseUrl || "").trim().replace(/\/+$/, "");
  cfg.casesVersion = String(cfg.casesVersion || "1").trim();
  cfg.devOtp = String(cfg.devOtp || DEFAULTS.devOtp).trim();
  if (!Array.isArray(cfg.workflowPersonas) || !cfg.workflowPersonas.length) {
    cfg.workflowPersonas = DEFAULTS.workflowPersonas.slice();
  }

  window.TESTPANEL_CONFIG = {
    ...cfg,
    save(next) {
      const merged = { ...cfg, ...(next || {}) };
      merged.baseUrl = String(merged.baseUrl || "").trim().replace(/\/+$/, "");
      merged.casesVersion = String(merged.casesVersion || "1").trim();
      merged.devOtp = String(merged.devOtp || DEFAULTS.devOtp).trim();
      if (!Array.isArray(merged.workflowPersonas) || !merged.workflowPersonas.length) {
        merged.workflowPersonas = DEFAULTS.workflowPersonas.slice();
      }
      localStorage.setItem(DEFAULTS.storageKey, JSON.stringify(merged));
      window.location.reload();
    },
  };
})();
