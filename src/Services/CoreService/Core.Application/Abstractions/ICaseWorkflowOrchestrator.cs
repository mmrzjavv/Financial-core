namespace Core.Application.Abstractions;

public interface ICaseWorkflowOrchestrator
{
    Task<string> StartAsync(Guid caseId, CancellationToken ct);
    Task SignalAsync(Guid caseId, string signal, object? payload, CancellationToken ct);
}
