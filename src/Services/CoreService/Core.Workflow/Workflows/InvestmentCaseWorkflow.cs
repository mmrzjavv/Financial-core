using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Core.Workflow.Activities;
using Core.Workflow.Signals;

namespace Core.Workflow.Workflows;

public sealed class InvestmentCaseWorkflow : WorkflowBase
{
    public const string DefinitionId = $"{nameof(Core)}.{nameof(Workflow)}.{nameof(Workflows)}.{nameof(InvestmentCaseWorkflow)}";

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        var caseIdInput = GetCaseId(builder);

        var start = new WriteLine("Workflow Started") { Id = "Start" };

        var waitDataEntry1 = new WaitForCaseSignalActivity { Id = "WaitDataEntry1", CaseId = caseIdInput, Signal = new(CoreWorkflowSignals.StatusChanged) };
        var waitReview1 = new WaitForCaseSignalActivity { Id = "WaitReview1", CaseId = caseIdInput, Signal = new(CoreWorkflowSignals.StatusChanged) };

        var waitDataEntry2 = new WaitForCaseSignalActivity { Id = "WaitDataEntry2", CaseId = caseIdInput, Signal = new(CoreWorkflowSignals.StatusChanged) };
        var waitReview2 = new WaitForCaseSignalActivity { Id = "WaitReview2", CaseId = caseIdInput, Signal = new(CoreWorkflowSignals.StatusChanged) };

        var waitInitialVal = new WaitForCaseSignalActivity { Id = "WaitInitialVal", CaseId = caseIdInput, Signal = new(CoreWorkflowSignals.StatusChanged) };
        var waitSecondaryVal = new WaitForCaseSignalActivity { Id = "WaitSecondaryVal", CaseId = caseIdInput, Signal = new(CoreWorkflowSignals.StatusChanged) };

        var waitLegalUpload = new WaitForCaseSignalActivity { Id = "WaitLegalUpload", CaseId = caseIdInput, Signal = new(CoreWorkflowSignals.StatusChanged) };
        var waitUserReview = new WaitForCaseSignalActivity { Id = "WaitUserReview", CaseId = caseIdInput, Signal = new(CoreWorkflowSignals.StatusChanged) };

        var waitDrafting = new WaitForCaseSignalActivity { Id = "WaitDrafting", CaseId = caseIdInput, Signal = new(CoreWorkflowSignals.StatusChanged) };
        var waitSignature = new WaitForCaseSignalActivity { Id = "WaitSignature", CaseId = caseIdInput, Signal = new(CoreWorkflowSignals.StatusChanged) };
        var waitFinalUpload = new WaitForCaseSignalActivity { Id = "WaitFinalUpload", CaseId = caseIdInput, Signal = new(CoreWorkflowSignals.StatusChanged) };

        var waitFinanceUpload = new WaitForCaseSignalActivity { Id = "WaitFinanceUpload", CaseId = caseIdInput, Signal = new(CoreWorkflowSignals.StatusChanged) };
        var waitFinanceReview = new WaitForCaseSignalActivity { Id = "WaitFinanceReview", CaseId = caseIdInput, Signal = new(CoreWorkflowSignals.StatusChanged) };

        var waitPayment = new WaitForCaseSignalActivity { Id = "WaitPayment", CaseId = caseIdInput, Signal = new(CoreWorkflowSignals.StatusChanged) };
        var end = new WriteLine("Workflow Completed") { Id = "End" };

        builder.Root = new Flowchart
        {
            Activities =
            {
                start,

                waitDataEntry1,
                waitReview1,

                waitDataEntry2,
                waitReview2,

                waitInitialVal,
                waitSecondaryVal,

                waitLegalUpload,
                waitUserReview,

                waitDrafting,
                waitSignature,
                waitFinalUpload,

                waitFinanceUpload,
                waitFinanceReview,

                waitPayment,
                end
            },
            Connections =
            {
                new Connection(start, waitDataEntry1),
                new Connection(waitDataEntry1, waitReview1),
                new Connection(waitReview1, waitDataEntry1),
                new Connection(waitReview1, waitDataEntry2),

                new Connection(waitDataEntry2, waitReview2),
                new Connection(waitReview2, waitDataEntry2),
                new Connection(waitReview2, waitInitialVal),

                new Connection(waitInitialVal, waitSecondaryVal),
                new Connection(waitSecondaryVal, waitLegalUpload),

                new Connection(waitLegalUpload, waitUserReview),
                new Connection(waitUserReview, waitLegalUpload),
                new Connection(waitUserReview, waitDrafting),

                new Connection(waitDrafting, waitSignature),
                new Connection(waitSignature, waitFinalUpload),
                new Connection(waitFinalUpload, waitFinanceUpload),

                new Connection(waitFinanceUpload, waitFinanceReview),
                new Connection(waitFinanceReview, waitFinanceUpload),
                new Connection(waitFinanceReview, waitPayment),

                new Connection(waitPayment, end)
            }
        };
    }

    private static Input<Guid> GetCaseId(IWorkflowBuilder builder) => new(ctx => ctx.GetInput<Guid>("CaseId"));
}
