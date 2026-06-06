using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Models;
using Core.Workflow.Activities;
using Core.Workflow.Signals;

namespace Core.Workflow.Workflows;

public sealed class LoanCaseWorkflow : WorkflowBase
{
    public const string DefinitionId = $"{nameof(Core)}.{nameof(Workflow)}.{nameof(Workflows)}.{nameof(LoanCaseWorkflow)}";

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        var caseIdInput = GetCaseId(builder);
        var start = new WriteLine("Loan workflow started") { Id = "Start" };
        var wait = new WaitForCaseSignalActivity
        {
            Id = "WaitSignal",
            CaseId = caseIdInput,
            Signal = new(CoreWorkflowSignals.StatusChanged)
        };
        var end = new WriteLine("Loan workflow completed") { Id = "End" };

        builder.Root = new Flowchart
        {
            Activities = { start, wait, end },
            Connections =
            {
                new Connection(start, wait),
                new Connection(wait, wait),
                new Connection(wait, end)
            }
        };
    }

    private static Input<Guid> GetCaseId(IWorkflowBuilder builder) => new(ctx => ctx.GetInput<Guid>("CaseId"));
}
