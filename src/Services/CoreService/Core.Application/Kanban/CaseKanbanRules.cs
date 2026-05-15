using Services.CoreService.Core.Domain.Constants;
using Services.CoreService.Core.Domain.Enums;

namespace Core.Application.Kanban;

/// <summary>
/// Role/status ownership for the kanban board. Keep aligned with CaseStateManager transitions.
/// </summary>
public static class CaseKanbanRules
{
    private static readonly CaseStatus[] TerminalStatuses =
    [
        CaseStatus.Completed,
        CaseStatus.Rejected,
        CaseStatus.Cancelled,
        CaseStatus.Archived
    ];

    private static readonly Dictionary<CaseStatus, string> StatusOwnerRole = new()
    {
        [CaseStatus.Draft] = SystemRoles.Applicant,
        [CaseStatus.DataEntry1] = SystemRoles.Applicant,
        [CaseStatus.DataEntry2] = SystemRoles.Applicant,
        [CaseStatus.ReviewDataEntry1] = SystemRoles.InvestmentExpert,
        [CaseStatus.ReviewDataEntry2] = SystemRoles.InvestmentExpert,
        [CaseStatus.InitialValuation] = SystemRoles.InvestmentManager,
        [CaseStatus.SecondaryValuation] = SystemRoles.InvestmentManager,
        [CaseStatus.WaitingPreliminaryContract] = SystemRoles.LegalExpert,
        [CaseStatus.WaitingUserReviewPreliminaryContract] = SystemRoles.Applicant,
        [CaseStatus.ContractDrafting] = SystemRoles.LegalExpert,
        [CaseStatus.WaitingContractSignature] = SystemRoles.LegalExpert,
        [CaseStatus.WaitingSignedContractUpload] = SystemRoles.LegalExpert,
        [CaseStatus.WaitingFinancialWorksheet] = SystemRoles.InvestmentExpert,
        [CaseStatus.FinancialWorksheetReview] = SystemRoles.FinancialExpert,
        [CaseStatus.WaitingPayment] = SystemRoles.FinancialExpert
    };

    private static readonly Dictionary<string, HashSet<CaseStatus>> ActionStatusesByRole = BuildActionStatusesByRole();

    private static readonly Dictionary<string, HashSet<CaseStatus>> WatchStatusesByRole = BuildWatchStatusesByRole();

    public static bool IsTerminal(CaseStatus status) => TerminalStatuses.Contains(status);

    public static string? GetStatusOwnerRole(CaseStatus status)
        => StatusOwnerRole.TryGetValue(status, out var role) ? role : null;

    public static string GetStatusTitle(CaseStatus status) =>
        KanbanStatusTitles.TryGetValue(status, out var title) ? title : status.ToString();

    public static string GetPhaseTitle(CasePhase phase) =>
        KanbanPhaseTitles.TryGetValue(phase, out var title) ? title : phase.ToString();

    public static bool IsActionRequired(CaseStatus status, string resolvedRole)
    {
        if (IsTerminal(status))
            return false;

        if (string.Equals(resolvedRole, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase))
            return StatusOwnerRole.ContainsKey(status);

        return ActionStatusesByRole.TryGetValue(resolvedRole, out var statuses) && statuses.Contains(status);
    }

    public static bool IsWatching(CaseStatus status, string resolvedRole)
    {
        if (IsTerminal(status) || IsActionRequired(status, resolvedRole))
            return false;

        if (string.Equals(resolvedRole, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase))
            return false;

        return WatchStatusesByRole.TryGetValue(resolvedRole, out var statuses) && statuses.Contains(status);
    }

    public static string GetPendingActionLabel(CaseStatus status, string resolvedRole)
    {
        if (IsActionRequired(status, resolvedRole))
            return KanbanActionHints.TryGetValue(status, out var hint) ? hint : "اقدام شما لازم است";

        var owner = GetStatusOwnerRole(status);
        if (owner is null)
            return "در جریان بررسی";

        return KanbanWaitingHints.TryGetValue((status, owner), out var waiting)
            ? waiting
            : $"در انتظار {RoleLabels.GetValueOrDefault(owner, owner)}";
    }

    public static string ResolveWorkflowRole(IReadOnlyCollection<string> roles)
    {
        if (roles.Contains(SystemRoles.Admin)) return SystemRoles.Admin;
        if (roles.Contains(SystemRoles.InvestmentManager)) return SystemRoles.InvestmentManager;
        if (roles.Contains(SystemRoles.InvestmentExpert)) return SystemRoles.InvestmentExpert;
        if (roles.Contains(SystemRoles.LegalExpert)) return SystemRoles.LegalExpert;
        if (roles.Contains(SystemRoles.FinancialExpert)) return SystemRoles.FinancialExpert;
        if (roles.Contains(SystemRoles.Applicant)) return SystemRoles.Applicant;
        return roles.FirstOrDefault() ?? string.Empty;
    }

    private static Dictionary<string, HashSet<CaseStatus>> BuildActionStatusesByRole()
    {
        var map = new Dictionary<string, HashSet<CaseStatus>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (status, role) in StatusOwnerRole)
        {
            if (!map.TryGetValue(role, out var set))
            {
                set = [];
                map[role] = set;
            }

            set.Add(status);
        }

        return map;
    }

    private static Dictionary<string, HashSet<CaseStatus>> BuildWatchStatusesByRole()
    {
        var active = Enum.GetValues<CaseStatus>().Where(s => !IsTerminal(s)).ToHashSet();

        HashSet<CaseStatus> WatchAllExcept(string role)
        {
            var action = ActionStatusesByRole[role];
            return active.Where(s => !action.Contains(s)).ToHashSet();
        }

        return new Dictionary<string, HashSet<CaseStatus>>(StringComparer.OrdinalIgnoreCase)
        {
            [SystemRoles.Applicant] = WatchAllExcept(SystemRoles.Applicant),
            [SystemRoles.InvestmentExpert] = WatchAllExcept(SystemRoles.InvestmentExpert),
            [SystemRoles.InvestmentManager] = WatchAllExcept(SystemRoles.InvestmentManager),
            [SystemRoles.LegalExpert] = WatchAllExcept(SystemRoles.LegalExpert),
            [SystemRoles.FinancialExpert] = WatchAllExcept(SystemRoles.FinancialExpert)
        };
    }

    private static readonly Dictionary<CaseStatus, string> KanbanStatusTitles = new()
    {
        [CaseStatus.Draft] = "پیش‌نویس",
        [CaseStatus.DataEntry1] = "فرم اولیه",
        [CaseStatus.ReviewDataEntry1] = "بررسی فرم اولیه",
        [CaseStatus.DataEntry2] = "فرم تکمیلی",
        [CaseStatus.ReviewDataEntry2] = "بررسی فرم تکمیلی",
        [CaseStatus.InitialValuation] = "ارزش‌گذاری اولیه",
        [CaseStatus.SecondaryValuation] = "ارزش‌گذاری ثانویه",
        [CaseStatus.WaitingPreliminaryContract] = "آپلود پیش‌قرارداد",
        [CaseStatus.WaitingUserReviewPreliminaryContract] = "بازبینی پیش‌قرارداد",
        [CaseStatus.ContractDrafting] = "تدوین قرارداد",
        [CaseStatus.WaitingContractSignature] = "امضای قرارداد",
        [CaseStatus.WaitingSignedContractUpload] = "آپلود قرارداد امضاشده",
        [CaseStatus.WaitingFinancialWorksheet] = "کاربرگ مالی",
        [CaseStatus.FinancialWorksheetReview] = "بررسی کاربرگ مالی",
        [CaseStatus.WaitingPayment] = "پرداخت",
        [CaseStatus.Completed] = "تکمیل‌شده",
        [CaseStatus.Rejected] = "رد شده",
        [CaseStatus.Cancelled] = "لغو شده",
        [CaseStatus.Archived] = "بایگانی"
    };

    private static readonly Dictionary<CasePhase, string> KanbanPhaseTitles = new()
    {
        [CasePhase.Application] = "درخواست",
        [CasePhase.Valuation] = "ارزش‌گذاری",
        [CasePhase.Legal] = "حقوقی",
        [CasePhase.Finance] = "مالی",
        [CasePhase.Closing] = "اختتام"
    };

    private static readonly Dictionary<CaseStatus, string> KanbanActionHints = new()
    {
        [CaseStatus.Draft] = "ارسال یا تکمیل پیش‌نویس",
        [CaseStatus.DataEntry1] = "تکمیل و ارسال فرم اولیه",
        [CaseStatus.DataEntry2] = "تکمیل و ارسال فرم تکمیلی",
        [CaseStatus.ReviewDataEntry1] = "بررسی و تأیید/اصلاح فرم اولیه",
        [CaseStatus.ReviewDataEntry2] = "بررسی و تأیید/اصلاح فرم تکمیلی",
        [CaseStatus.InitialValuation] = "ثبت/تأیید ارزش‌گذاری اولیه",
        [CaseStatus.SecondaryValuation] = "ثبت/تأیید ارزش‌گذاری ثانویه",
        [CaseStatus.WaitingPreliminaryContract] = "بارگذاری پیش‌قرارداد",
        [CaseStatus.WaitingUserReviewPreliminaryContract] = "بازبینی پیش‌قرارداد",
        [CaseStatus.ContractDrafting] = "تدوین پیش‌نویس قرارداد",
        [CaseStatus.WaitingContractSignature] = "تأیید آماده‌سازی امضا",
        [CaseStatus.WaitingSignedContractUpload] = "بارگذاری قرارداد امضاشده",
        [CaseStatus.WaitingFinancialWorksheet] = "تکمیل و ارسال کاربرگ مالی",
        [CaseStatus.FinancialWorksheetReview] = "بررسی کاربرگ مالی",
        [CaseStatus.WaitingPayment] = "ثبت/تأیید پرداخت"
    };

    private static readonly Dictionary<(CaseStatus Status, string OwnerRole), string> KanbanWaitingHints = new()
    {
        [(CaseStatus.ReviewDataEntry1, SystemRoles.InvestmentExpert)] = "در انتظار بررسی کارشناس سرمایه‌گذاری",
        [(CaseStatus.ReviewDataEntry2, SystemRoles.InvestmentExpert)] = "در انتظار بررسی کارشناس سرمایه‌گذاری",
        [(CaseStatus.InitialValuation, SystemRoles.InvestmentManager)] = "در انتظار ارزش‌گذاری مدیر",
        [(CaseStatus.SecondaryValuation, SystemRoles.InvestmentManager)] = "در انتظار ارزش‌گذاری مدیر",
        [(CaseStatus.WaitingPreliminaryContract, SystemRoles.LegalExpert)] = "در انتظار واحد حقوقی",
        [(CaseStatus.WaitingUserReviewPreliminaryContract, SystemRoles.Applicant)] = "در انتظار متقاضی",
        [(CaseStatus.FinancialWorksheetReview, SystemRoles.FinancialExpert)] = "در انتظار واحد مالی"
    };

    private static readonly Dictionary<string, string> RoleLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        [SystemRoles.Applicant] = "متقاضی",
        [SystemRoles.InvestmentExpert] = "کارشناس سرمایه‌گذاری",
        [SystemRoles.InvestmentManager] = "مدیر سرمایه‌گذاری",
        [SystemRoles.LegalExpert] = "کارشناس حقوقی",
        [SystemRoles.FinancialExpert] = "کارشناس مالی",
        [SystemRoles.Admin] = "مدیر سیستم"
    };
}
