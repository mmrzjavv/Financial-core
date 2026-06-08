using Core.Application.Kanban;
using Core.Domain.Identity;

namespace Core.Application.Dashboard;

public static class DashboardKanbanStatusFilters
{
    public static IReadOnlyList<int> GetInvestmentQueueStatuses(string departmentKey)
    {
        var role = DashboardRoleResolver.GetDepartmentRepresentativeRole(departmentKey);
        return CaseKanbanRules.GetActionRequiredStatusValues(role);
    }

    public static IReadOnlyList<int> GetGuaranteeQueueStatuses(string departmentKey)
    {
        var role = DashboardRoleResolver.GetDepartmentRepresentativeRole(departmentKey);
        return GuaranteeKanbanRules.GetActionRequiredStatusValues(role);
    }

    public static IReadOnlyList<int> GetLoanQueueStatuses(string departmentKey)
    {
        var role = DashboardRoleResolver.GetDepartmentRepresentativeRole(departmentKey);
        return LoanKanbanRules.GetActionRequiredStatusValues(role);
    }
}
