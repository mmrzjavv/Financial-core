using Core.Application.Abstractions;
using Core.Workflow.Activities;
using Core.Workflow.Workflows;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Requests;
using Services.CoreService.Core.Domain.Entities;

namespace Core.Workflow.Orchestration;

public sealed class ElsaCaseWorkflowOrchestrator(
    IInvestmentCaseRepository investmentCaseRepository,
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowDispatcher workflowDispatcher,
    IWorkflowResumer workflowResumer) : ICaseWorkflowOrchestrator
{
    public async Task<string> StartAsync(Guid caseId, CancellationToken ct)
    {
        var definition = await workflowDefinitionService.FindWorkflowDefinitionAsync(
            InvestmentCaseWorkflow.DefinitionId,
            VersionOptions.Latest,
            ct);

        if (definition is null)
            throw new InvalidOperationException($"Workflow definition '{InvestmentCaseWorkflow.DefinitionId}' was not found.");

        var instanceId = caseId.ToString("D");
        var request = new DispatchWorkflowDefinitionRequest(definition.Id)
        {
            Input = new Dictionary<string, object> { ["CaseId"] = caseId },
            InstanceId = instanceId,
            CorrelationId = instanceId
        };

        await workflowDispatcher.DispatchAsync(request, ct);
        return instanceId;
    }

    public async Task SignalAsync(Guid caseId, string signal, object? payload, CancellationToken ct)
    {
        var investmentCase = await investmentCaseRepository.GetAsync(caseId, ct)
            ?? throw new InvalidOperationException($"Investment case '{caseId}' was not found.");

        if (string.IsNullOrEmpty(investmentCase.WorkflowInstanceId))
            throw new InvalidOperationException($"Case '{caseId}' has no workflow instance.");

        var stimulus = new Dictionary<string, object>
        {
            ["CaseId"] = caseId,
            ["Signal"] = signal
        };

        IDictionary<string, object>? input = null;
        if (payload is not null)
            input = new Dictionary<string, object> { ["SignalPayload"] = payload };

        var responses = await workflowResumer.ResumeAsync<WaitForCaseSignalActivity>(
            stimulus,
            investmentCase.WorkflowInstanceId,
            new ResumeBookmarkOptions { Input = input },
            ct);

        if (!responses.Any())
            throw new InvalidOperationException(
                $"No workflow bookmark matched signal '{signal}' for case '{caseId}'.");
    }
}
