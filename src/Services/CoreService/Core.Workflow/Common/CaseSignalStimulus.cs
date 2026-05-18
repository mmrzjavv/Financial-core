namespace Core.Workflow.Common;

/// <summary>
/// Bookmark/resume stimulus for <see cref="Activities.WaitForCaseSignalActivity"/>.
/// Must not be a dictionary — Elsa's hash serializer reflects all properties and fails on Dictionary indexers.
/// </summary>
public sealed class CaseSignalStimulus
{
    public Guid CaseId { get; init; }
    public string Signal { get; init; } = string.Empty;
}
