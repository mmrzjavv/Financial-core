using Core.Domain.Enums;

namespace Core.Workflow.Signals;

public static class CaseWorkflowSignals
{
    public static string For(CasePhase phase, string action) => $"case.{phase}.{action}".ToLowerInvariant();
}
