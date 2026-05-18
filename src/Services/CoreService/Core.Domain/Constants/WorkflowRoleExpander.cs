using Core.Domain.Enums;

namespace Core.Domain.Constants;

/// <summary>Duplicates workflow transitions and kanban ownership from unit expert to unit manager.</summary>
public static class WorkflowRoleExpander
{
    public static void MirrorUnitManager(
        IDictionary<(CaseStatus Current, WorkflowAction Action, string Role), CaseStatus> transitions,
        string expertRole,
        string managerRole)
    {
        var additions = transitions
            .Where(kv => string.Equals(kv.Key.Role, expertRole, StringComparison.OrdinalIgnoreCase))
            .Select(kv => ((kv.Key.Current, kv.Key.Action, managerRole), kv.Value))
            .ToList();

        foreach (var (key, value) in additions)
            transitions.TryAdd(key, value);
    }

    public static void MirrorKanbanRole(
        IDictionary<string, HashSet<CaseStatus>> map,
        string fromRole,
        string toRole)
    {
        if (!map.TryGetValue(fromRole, out var statuses))
            return;

        map[toRole] = statuses.ToHashSet();
    }
}
