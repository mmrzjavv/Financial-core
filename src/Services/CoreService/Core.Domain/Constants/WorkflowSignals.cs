namespace Core.Domain.Constants;

public static class WorkflowSignals
{
    public const string CaseSubmitted = "case-submitted";
    public const string RevisionRequested = "revision-requested";
    public const string StatusChanged = "status-changed";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string PaymentCompleted = "payment-completed";
    public const string ContractSigned = "contract-signed";
}
