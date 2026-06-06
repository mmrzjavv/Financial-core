using Core.Domain.Constants;
using Core.Domain.Enums;
using Core.Domain.Identity;

namespace Core.Application.Kanban;

public static class LoanKanbanRules
{
    private static readonly LoanCaseStatus[] TerminalStatuses =
    [
        LoanCaseStatus.Completed,
        LoanCaseStatus.CanceledByCeo,
        LoanCaseStatus.Archived
    ];

    private static readonly Dictionary<LoanCaseStatus, string> StatusOwnerRole = new()
    {
        [LoanCaseStatus.Draft] = UserRoleClaims.Applicant,
        [LoanCaseStatus.DataEntry] = UserRoleClaims.Applicant,
        [LoanCaseStatus.PendingCreditReview] = UserRoleClaims.CreditExpert,
        [LoanCaseStatus.RevisionRequestedByCredit] = UserRoleClaims.Applicant,
        [LoanCaseStatus.PendingCeoInitialApproval] = UserRoleClaims.Ceo,
        [LoanCaseStatus.PendingLegalRawContract] = UserRoleClaims.LegalExpert,
        [LoanCaseStatus.PendingApplicantSignature] = UserRoleClaims.Applicant,
        [LoanCaseStatus.PendingLegalFinalReview] = UserRoleClaims.LegalExpert,
        [LoanCaseStatus.RevisionRequestedByLegal] = UserRoleClaims.Applicant,
        [LoanCaseStatus.PendingFinancialReview] = UserRoleClaims.FinancialExpert,
        [LoanCaseStatus.RevisionRequestedByFinancial] = UserRoleClaims.Applicant,
        [LoanCaseStatus.PendingLegalFinalContract] = UserRoleClaims.LegalExpert,
        [LoanCaseStatus.PendingCeoFinalApproval] = UserRoleClaims.Ceo,
        [LoanCaseStatus.ReadyForPayment] = UserRoleClaims.FinancialExpert,
        [LoanCaseStatus.RepaymentPhase] = UserRoleClaims.Applicant
    };

    private static readonly Dictionary<string, HashSet<LoanCaseStatus>> ActionStatusesByRole = BuildActionStatusesByRole();
    private static readonly Dictionary<string, HashSet<LoanCaseStatus>> WatchStatusesByRole = BuildWatchStatusesByRole();

    public static bool IsTerminal(LoanCaseStatus status) => TerminalStatuses.Contains(status);

    public static bool IsActionRequired(LoanCaseStatus status, string resolvedRole)
    {
        if (IsTerminal(status)) return false;
        if (string.Equals(resolvedRole, UserRoleClaims.Admin, StringComparison.OrdinalIgnoreCase))
            return StatusOwnerRole.ContainsKey(status);
        return ActionStatusesByRole.TryGetValue(resolvedRole, out var statuses) && statuses.Contains(status);
    }

    public static bool IsWatching(LoanCaseStatus status, string resolvedRole)
    {
        if (IsTerminal(status) || IsActionRequired(status, resolvedRole)) return false;
        if (string.Equals(resolvedRole, UserRoleClaims.Admin, StringComparison.OrdinalIgnoreCase)) return false;
        return WatchStatusesByRole.TryGetValue(resolvedRole, out var statuses) && statuses.Contains(status);
    }

    public static string GetStatusTitle(LoanCaseStatus status) => status switch
    {
        LoanCaseStatus.Draft => "پیش‌نویس",
        LoanCaseStatus.DataEntry => "ورود اطلاعات",
        LoanCaseStatus.PendingCreditReview => "بررسی اعتبارات",
        LoanCaseStatus.RevisionRequestedByCredit => "اصلاح درخواست (اعتبارات)",
        LoanCaseStatus.PendingCeoInitialApproval => "تأیید مدیرعامل (اول)",
        LoanCaseStatus.CanceledByCeo => "لغو شده توسط مدیرعامل",
        LoanCaseStatus.PendingLegalRawContract => "قرارداد خام و اقساط",
        LoanCaseStatus.PendingApplicantSignature => "امضای متقاضی",
        LoanCaseStatus.PendingLegalFinalReview => "بررسی نهایی حقوقی",
        LoanCaseStatus.RevisionRequestedByLegal => "اصلاح درخواست (حقوقی)",
        LoanCaseStatus.PendingFinancialReview => "بررسی مالی",
        LoanCaseStatus.RevisionRequestedByFinancial => "اصلاح درخواست (مالی)",
        LoanCaseStatus.PendingLegalFinalContract => "قرارداد نهایی",
        LoanCaseStatus.PendingCeoFinalApproval => "تأیید مدیرعامل (نهایی)",
        LoanCaseStatus.ReadyForPayment => "آماده پرداخت",
        LoanCaseStatus.RepaymentPhase => "فاز بازپرداخت",
        LoanCaseStatus.Completed => "تکمیل‌شده",
        LoanCaseStatus.Archived => "بایگانی",
        _ => status.ToString()
    };

    public static string GetPhaseTitle(LoanCasePhase phase) => phase switch
    {
        LoanCasePhase.Application => "درخواست",
        LoanCasePhase.CreditAssessment => "اعتبارات",
        LoanCasePhase.Legal => "حقوقی",
        LoanCasePhase.Finance => "مالی",
        LoanCasePhase.Repayment => "بازپرداخت",
        LoanCasePhase.Closing => "اختتام",
        _ => phase.ToString()
    };

    public static string ResolveWorkflowRole(IReadOnlyCollection<string> roles)
    {
        if (roles.Contains(UserRoleClaims.Admin)) return UserRoleClaims.Admin;
        if (roles.Contains(UserRoleClaims.Ceo) || roles.Contains("CEO", StringComparer.OrdinalIgnoreCase))
            return UserRoleClaims.Ceo;
        if (roles.Contains(UserRoleClaims.CreditManager)) return UserRoleClaims.CreditManager;
        if (roles.Contains(UserRoleClaims.CreditExpert)) return UserRoleClaims.CreditExpert;
        if (roles.Contains(UserRoleClaims.LegalManager)) return UserRoleClaims.LegalManager;
        if (roles.Contains(UserRoleClaims.LegalExpert)) return UserRoleClaims.LegalExpert;
        if (roles.Contains(UserRoleClaims.FinancialManager)) return UserRoleClaims.FinancialManager;
        if (roles.Contains(UserRoleClaims.FinancialExpert)) return UserRoleClaims.FinancialExpert;
        if (roles.Contains(UserRoleClaims.Applicant)) return UserRoleClaims.Applicant;
        return roles.FirstOrDefault() ?? string.Empty;
    }

    public static string GetPendingActionLabel(LoanCaseStatus status, string resolvedRole)
    {
        if (IsActionRequired(status, resolvedRole))
            return "اقدام شما لازم است";

        if (StatusOwnerRole.TryGetValue(status, out var owner))
        {
            var ownerLabel = owner switch
            {
                UserRoleClaims.Applicant => "متقاضی",
                UserRoleClaims.CreditExpert or UserRoleClaims.CreditManager => "اعتبارات",
                UserRoleClaims.LegalExpert or UserRoleClaims.LegalManager => "حقوقی",
                UserRoleClaims.FinancialExpert or UserRoleClaims.FinancialManager => "مالی",
                UserRoleClaims.Ceo => "مدیرعامل",
                _ => owner
            };
            return $"در انتظار {ownerLabel}";
        }

        return "در جریان بررسی";
    }

    private static Dictionary<string, HashSet<LoanCaseStatus>> BuildActionStatusesByRole()
    {
        var map = new Dictionary<string, HashSet<LoanCaseStatus>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (status, role) in StatusOwnerRole)
        {
            if (!map.TryGetValue(role, out var set))
            {
                set = [];
                map[role] = set;
            }
            set.Add(status);
        }
        WorkflowRoleExpander.MirrorKanbanRole(map, UserRoleClaims.CreditExpert, UserRoleClaims.CreditManager);
        WorkflowRoleExpander.MirrorKanbanRole(map, UserRoleClaims.LegalExpert, UserRoleClaims.LegalManager);
        WorkflowRoleExpander.MirrorKanbanRole(map, UserRoleClaims.FinancialExpert, UserRoleClaims.FinancialManager);
        return map;
    }

    private static Dictionary<string, HashSet<LoanCaseStatus>> BuildWatchStatusesByRole()
    {
        var all = Enum.GetValues<LoanCaseStatus>().Where(s => !IsTerminal(s)).ToHashSet();
        var watch = new Dictionary<string, HashSet<LoanCaseStatus>>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in new[] { UserRoleClaims.Ceo, UserRoleClaims.CreditExpert, UserRoleClaims.LegalExpert, UserRoleClaims.FinancialExpert, UserRoleClaims.Applicant })
        {
            if (!ActionStatusesByRole.TryGetValue(role, out var owned))
                watch[role] = all;
            else
                watch[role] = all.Where(s => !owned.Contains(s)).ToHashSet();
        }
        WorkflowRoleExpander.MirrorKanbanRole(watch, UserRoleClaims.CreditExpert, UserRoleClaims.CreditManager);
        WorkflowRoleExpander.MirrorKanbanRole(watch, UserRoleClaims.LegalExpert, UserRoleClaims.LegalManager);
        WorkflowRoleExpander.MirrorKanbanRole(watch, UserRoleClaims.FinancialExpert, UserRoleClaims.FinancialManager);
        return watch;
    }
}
