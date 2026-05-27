namespace Core.Application.Abstractions;

public interface IGuaranteeWorkflowOrchestrator
{
    Task<string> StartGuaranteeCaseAsync(Guid caseId, CancellationToken ct);
    Task<string> StartRenewalCaseAsync(Guid caseId, CancellationToken ct);
    Task SignalGuaranteeCaseAsync(Guid caseId, string signal, object? payload, CancellationToken ct);
    Task SignalRenewalCaseAsync(Guid caseId, string signal, object? payload, CancellationToken ct);
}
