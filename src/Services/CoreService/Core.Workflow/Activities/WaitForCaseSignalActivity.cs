using Elsa.Workflows;
using Elsa.Workflows.Models;

namespace Core.Workflow.Activities;

public sealed class WaitForCaseSignalActivity : Elsa.Workflows.Activity
{
    public Input<Guid> CaseId { get; set; } = default!;
    public Input<string> Signal { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var caseId = context.Get(CaseId);
        var signal = context.Get(Signal);

        var stimulus = new Dictionary<string, object>
        {
            ["CaseId"] = caseId,
            ["Signal"] = signal ?? string.Empty
        };

        var bookmarkArgs = new CreateBookmarkArgs
        {
            Stimulus = stimulus,
            AutoBurn = true,
            IncludeActivityInstanceId = false,
            Callback = OnResumeAsync
        };

        context.CreateBookmark(bookmarkArgs);
        await ValueTask.CompletedTask;
    }

    private static async ValueTask OnResumeAsync(ActivityExecutionContext context)
    {
        await context.CompleteActivityWithOutcomesAsync("Done");
    }
}
