namespace Core.Application.Abstractions;

public interface ILoanWorkflowOrchestrator
{
    Task<string> StartLoanCaseAsync(Guid caseId, CancellationToken ct);
    Task SignalLoanCaseAsync(Guid caseId, string signal, object? payload, CancellationToken ct);
}
