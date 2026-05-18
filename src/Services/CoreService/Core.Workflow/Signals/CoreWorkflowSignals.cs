using Core.Domain.Constants;

namespace Core.Workflow.Signals;

public static class CoreWorkflowSignals
{
    public const string StatusChanged = WorkflowSignals.StatusChanged;
    public const string RevisionRequested = WorkflowSignals.RevisionRequested;
}
